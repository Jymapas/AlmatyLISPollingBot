using AlmatyLISPollingBot.Application.Features.MakePost;
using AlmatyLISPollingBot.Application.Features.ExcludedTournaments;
using AlmatyLISPollingBot.Application.Features.Polls.Options;
using AlmatyLISPollingBot.Application.Features.Polls.StartPoll;
using AlmatyLISPollingBot.Application.Features.Polls.StopPoll;
using AlmatyLISPollingBot.Application.Abstractions.Tournaments;
using AlmatyLISPollingBot.Application.Contracts.Tournaments;
using AlmatyLISPollingBot.Application.Contracts.Bot;
using AlmatyLISPollingBot.Application.Contracts.Polls;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AlmatyLISPollingBot.Worker.Telegram;

public sealed class TelegramUpdateRouter
{
    private const int TelegramMessageMaxLength = 4096;

    private readonly StartPollService startPollService;
    private readonly ListTournamentOptionsService listTournamentOptionsService;
    private readonly StopPollService stopPollService;
    private readonly MakePostService makePostService;
    private readonly ExcludeTournamentsService excludeTournamentsService;
    private readonly PollCommandAuthorizer pollCommandAuthorizer;
    private readonly IExcludeDialogState excludeDialogState;
    private readonly IChgkTournamentClient tournamentClient;
    private readonly ITelegramBotClient botClient;
    private readonly ILogger<TelegramUpdateRouter> logger;

    public TelegramUpdateRouter(
        StartPollService startPollService,
        ListTournamentOptionsService listTournamentOptionsService,
        StopPollService stopPollService,
        MakePostService makePostService,
        ExcludeTournamentsService excludeTournamentsService,
        PollCommandAuthorizer pollCommandAuthorizer,
        IExcludeDialogState excludeDialogState,
        IChgkTournamentClient tournamentClient,
        ITelegramBotClient botClient,
        ILogger<TelegramUpdateRouter> logger)
    {
        this.startPollService = startPollService;
        this.listTournamentOptionsService = listTournamentOptionsService;
        this.stopPollService = stopPollService;
        this.makePostService = makePostService;
        this.excludeTournamentsService = excludeTournamentsService;
        this.pollCommandAuthorizer = pollCommandAuthorizer;
        this.excludeDialogState = excludeDialogState;
        this.tournamentClient = tournamentClient;
        this.botClient = botClient;
        this.logger = logger;
    }

    public async Task RouteAsync(Update update, CancellationToken cancellationToken)
    {
        var message = update.Message;
        var messageText = message?.Text;
        var user = message?.From;
        if (string.IsNullOrWhiteSpace(messageText) || user is null || message is null)
        {
            return;
        }

        var commandContext = new PollCommandContext(
            message.Chat.Id,
            user.Id,
            message.Chat.Type == ChatType.Private);

        if (await IsBotCommandAsync(messageText, BotCommands.Exclude, cancellationToken))
        {
            if (!commandContext.IsPrivateChat
                || !await pollCommandAuthorizer.IsAuthorizedAsync(commandContext, cancellationToken))
            {
                return;
            }

            var payload = GetCommandPayload(messageText);
            if (string.IsNullOrWhiteSpace(payload))
            {
                excludeDialogState.Start(user.Id);
                await SendPrivateMessageAsync(
                    message.Chat.Id,
                    "Перечислите ID или ссылки на турниры, которые нужно исключить. Для отмены отправьте /cancel.",
                    cancellationToken);
                return;
            }

            excludeDialogState.Cancel(user.Id);
            await ProcessExclusionAsync(message.Chat.Id, user.Id, payload, cancellationToken);
            return;
        }

        if (await IsBotCommandAsync(messageText, BotCommands.Cancel, cancellationToken))
        {
            if (!commandContext.IsPrivateChat
                || !await pollCommandAuthorizer.IsAuthorizedAsync(commandContext, cancellationToken))
            {
                return;
            }

            var wasCancelled = excludeDialogState.Cancel(user.Id);
            await SendPrivateMessageAsync(
                message.Chat.Id,
                wasCancelled ? "Диалог исключения турниров отменён." : "Нет активного диалога для отмены.",
                cancellationToken);
            return;
        }

        if (await IsBotCommandAsync(messageText, BotCommands.Poll, cancellationToken))
        {
            if (!await pollCommandAuthorizer.IsAuthorizedAsync(commandContext, cancellationToken))
            {
                return;
            }

            await startPollService.StartAsync(cancellationToken);
            logger.LogInformation("Received /poll command.");
            return;
        }

        if (await IsBotCommandAsync(messageText, BotCommands.Options, cancellationToken))
        {
            if (!commandContext.IsPrivateChat
                || !await pollCommandAuthorizer.IsAuthorizedAsync(commandContext, cancellationToken))
            {
                return;
            }

            var result = await listTournamentOptionsService.ExecuteAsync(cancellationToken);
            if (result.Pages.Count == 0)
            {
                await SendPrivateMessageAsync(
                    message.Chat.Id,
                    $"Не найдено подходящих турниров на {result.TargetDate:dd.MM.yyyy}.",
                    cancellationToken);
            }
            else
            {
                foreach (var page in result.Pages)
                {
                    await SendHtmlPrivateMessageAsync(message.Chat.Id, page, cancellationToken);
                }
            }

            logger.LogInformation("Received /options command.");
            return;
        }

        if (await IsBotCommandAsync(messageText, "stop", cancellationToken))
        {
            if (!await pollCommandAuthorizer.IsAuthorizedAsync(commandContext, cancellationToken))
            {
                return;
            }

            await stopPollService.StopActivePollAsync(cancellationToken);
            logger.LogInformation("Received /stop command.");
            return;
        }

        if (await IsBotCommandAsync(messageText, "makepost", cancellationToken))
        {
            if (!commandContext.IsPrivateChat
                || !await pollCommandAuthorizer.IsAuthorizedAsync(commandContext, cancellationToken))
            {
                return;
            }

            var payload = GetCommandPayload(messageText);
            await makePostService.GenerateDraftAsync(MakePostRequest.Parse(payload), cancellationToken);
            logger.LogInformation("Received /makepost command.");
            return;
        }

        if (commandContext.IsPrivateChat
            && await pollCommandAuthorizer.IsAuthorizedAsync(commandContext, cancellationToken)
            && excludeDialogState.IsAwaitingInput(user.Id))
        {
            await ProcessExclusionAsync(message.Chat.Id, user.Id, messageText, cancellationToken);
        }
    }

    private async Task ProcessExclusionAsync(
        long chatId,
        long userId,
        string input,
        CancellationToken cancellationToken)
    {
        var result = await excludeTournamentsService.ExecuteAsync(input, cancellationToken);
        if (!result.IsValid)
        {
            var errorMessage = result.IsEmptyInput
                ? "Укажите хотя бы один ID или ссылку на турнир."
                : $"Не удалось распознать: {string.Join(", ", result.InvalidTokens)}. Укажите ID или ссылки на турниры.";
            await SendPrivateMessageAsync(chatId, errorMessage, cancellationToken);
            logger.LogInformation(
                "Rejected tournament exclusion input from Telegram user {TelegramUserId} in chat {ChatId}. Invalid token count: {InvalidTokenCount}.",
                userId,
                chatId,
                result.InvalidTokens.Count);
            return;
        }

        excludeDialogState.Cancel(userId);
        var excludedTournamentIds = result.AddedTournamentIds
            .Concat(result.AlreadyExcludedTournamentIds)
            .ToArray();
        var tournaments = await GetTournamentDetailsAsync(excludedTournamentIds, cancellationToken);
        await SendHtmlPrivateMessageAsync(
            chatId,
            ExcludedTournamentResultFormatter.Format(result, tournaments),
            cancellationToken);
        logger.LogInformation(
            "Processed tournament exclusions for Telegram user {TelegramUserId} in chat {ChatId}. Added: {AddedCount}; already excluded: {AlreadyExcludedCount}.",
            userId,
            chatId,
            result.AddedTournamentIds.Count,
            result.AlreadyExcludedTournamentIds.Count);
    }

    private Task SendPrivateMessageAsync(long chatId, string text, CancellationToken cancellationToken)
    {
        return botClient.SendMessage(chatId, TruncateTelegramMessage(text), cancellationToken: cancellationToken);
    }

    private Task SendHtmlPrivateMessageAsync(long chatId, string html, CancellationToken cancellationToken)
    {
        return botClient.SendMessage(chatId, html, parseMode: ParseMode.Html, cancellationToken: cancellationToken);
    }

    private static string TruncateTelegramMessage(string text)
    {
        return text.Length <= TelegramMessageMaxLength
            ? text
            : string.Concat(text.AsSpan(0, TelegramMessageMaxLength - 1), "…");
    }

    private async Task<IReadOnlyCollection<TournamentDetails>> GetTournamentDetailsAsync(
        IReadOnlyCollection<int> tournamentIds,
        CancellationToken cancellationToken)
    {
        try
        {
            return await tournamentClient.GetTournamentsByIdsAsync(tournamentIds, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(
                exception,
                "Could not load tournament titles for {TournamentCount} excluded tournaments.",
                tournamentIds.Count);
            return Array.Empty<TournamentDetails>();
        }
    }

    private async Task<bool> IsBotCommandAsync(string messageText, string commandName, CancellationToken cancellationToken)
    {
        var commandToken = GetCommandToken(messageText);
        var command = BotCommands.ToMessageCommand(commandName);
        if (string.Equals(commandToken, command, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var commandWithUsernamePrefix = string.Concat(command, "@");
        if (!commandToken.StartsWith(commandWithUsernamePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var botUsername = (await botClient.GetMe(cancellationToken)).Username;
        return !string.IsNullOrWhiteSpace(botUsername)
            && string.Equals(
                commandToken[commandWithUsernamePrefix.Length..],
                botUsername,
                StringComparison.OrdinalIgnoreCase);
    }

    private static string GetCommandToken(string messageText)
    {
        var separatorIndex = messageText.IndexOfAny(new[] { ' ', '\n', '\r', '\t' });
        return separatorIndex < 0 ? messageText : messageText[..separatorIndex];
    }

    private static string GetCommandPayload(string messageText)
    {
        var commandToken = GetCommandToken(messageText);
        return messageText[commandToken.Length..].Trim();
    }
}
