using AlmatyLISPollingBot.Application.Features.MakePost;
using AlmatyLISPollingBot.Application.Features.ExcludedTournaments;
using AlmatyLISPollingBot.Application.Features.Polls.StartPoll;
using AlmatyLISPollingBot.Application.Features.Polls.StopPoll;
using AlmatyLISPollingBot.Application.Contracts.Bot;
using AlmatyLISPollingBot.Application.Contracts.Polls;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AlmatyLISPollingBot.Worker.Telegram;

public sealed class TelegramUpdateRouter
{
    private readonly StartPollService startPollService;
    private readonly StopPollService stopPollService;
    private readonly MakePostService makePostService;
    private readonly ExcludeTournamentsService excludeTournamentsService;
    private readonly PollCommandAuthorizer pollCommandAuthorizer;
    private readonly IExcludeDialogState excludeDialogState;
    private readonly ITelegramBotClient botClient;
    private readonly ILogger<TelegramUpdateRouter> logger;

    public TelegramUpdateRouter(
        StartPollService startPollService,
        StopPollService stopPollService,
        MakePostService makePostService,
        ExcludeTournamentsService excludeTournamentsService,
        PollCommandAuthorizer pollCommandAuthorizer,
        IExcludeDialogState excludeDialogState,
        ITelegramBotClient botClient,
        ILogger<TelegramUpdateRouter> logger)
    {
        this.startPollService = startPollService;
        this.stopPollService = stopPollService;
        this.makePostService = makePostService;
        this.excludeTournamentsService = excludeTournamentsService;
        this.pollCommandAuthorizer = pollCommandAuthorizer;
        this.excludeDialogState = excludeDialogState;
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
        await SendPrivateMessageAsync(chatId, FormatExclusionResult(result), cancellationToken);
        logger.LogInformation(
            "Processed tournament exclusions for Telegram user {TelegramUserId} in chat {ChatId}. Added: {AddedCount}; already excluded: {AlreadyExcludedCount}.",
            userId,
            chatId,
            result.AddedTournamentIds.Count,
            result.AlreadyExcludedTournamentIds.Count);
    }

    private Task SendPrivateMessageAsync(long chatId, string text, CancellationToken cancellationToken)
    {
        return botClient.SendMessage(chatId, text, cancellationToken: cancellationToken);
    }

    private static string FormatExclusionResult(ExcludeTournamentsResult result)
    {
        var messageParts = new List<string>();
        if (result.AddedTournamentIds.Count > 0)
        {
            messageParts.Add($"Исключены из будущих опросов: {string.Join(", ", result.AddedTournamentIds)}.");
        }

        if (result.AlreadyExcludedTournamentIds.Count > 0)
        {
            messageParts.Add($"Уже были исключены: {string.Join(", ", result.AlreadyExcludedTournamentIds)}.");
        }

        return string.Join("\n", messageParts);
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
