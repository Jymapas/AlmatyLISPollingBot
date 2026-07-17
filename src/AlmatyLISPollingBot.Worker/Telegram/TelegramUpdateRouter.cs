using AlmatyLISPollingBot.Application.Features.MakePost;
using AlmatyLISPollingBot.Application.Features.Administrators;
using AlmatyLISPollingBot.Application.Features.ExcludedTournaments;
using AlmatyLISPollingBot.Application.Features.ForcedTournaments;
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
using AlmatyLISPollingBot.Worker.HostedServices;
using Microsoft.Extensions.Options;

namespace AlmatyLISPollingBot.Worker.Telegram;

public sealed class TelegramUpdateRouter
{
    private const int TelegramMessageMaxLength = 4096;

    private readonly StartPollService startPollService;
    private readonly ListTournamentOptionsService listTournamentOptionsService;
    private readonly StopPollService stopPollService;
    private readonly MakePostService makePostService;
    private readonly ExcludeTournamentsService excludeTournamentsService;
    private readonly ForceTournamentsService forceTournamentsService;
    private readonly UpdateSettingsService updateSettingsService;
    private readonly PollCommandAuthorizer pollCommandAuthorizer;
    private readonly TelegramCommandMenuInitializationService commandMenuInitializationService;
    private readonly IOptions<BotConfiguration> botConfiguration;
    private readonly IPrivateAdminDialogState privateAdminDialogState;
    private readonly IChgkTournamentClient tournamentClient;
    private readonly ITelegramBotClient botClient;
    private readonly ILogger<TelegramUpdateRouter> logger;

    public TelegramUpdateRouter(
        StartPollService startPollService,
        ListTournamentOptionsService listTournamentOptionsService,
        StopPollService stopPollService,
        MakePostService makePostService,
        ExcludeTournamentsService excludeTournamentsService,
        ForceTournamentsService forceTournamentsService,
        UpdateSettingsService updateSettingsService,
        PollCommandAuthorizer pollCommandAuthorizer,
        TelegramCommandMenuInitializationService commandMenuInitializationService,
        IOptions<BotConfiguration> botConfiguration,
        IPrivateAdminDialogState privateAdminDialogState,
        IChgkTournamentClient tournamentClient,
        ITelegramBotClient botClient,
        ILogger<TelegramUpdateRouter> logger)
    {
        this.startPollService = startPollService;
        this.listTournamentOptionsService = listTournamentOptionsService;
        this.stopPollService = stopPollService;
        this.makePostService = makePostService;
        this.excludeTournamentsService = excludeTournamentsService;
        this.forceTournamentsService = forceTournamentsService;
        this.updateSettingsService = updateSettingsService;
        this.pollCommandAuthorizer = pollCommandAuthorizer;
        this.commandMenuInitializationService = commandMenuInitializationService;
        this.botConfiguration = botConfiguration;
        this.privateAdminDialogState = privateAdminDialogState;
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

        if (await IsBotCommandAsync(messageText, BotCommands.UpdateSettings, cancellationToken))
        {
            if (!commandContext.IsPrivateChat
                || commandContext.UserId != botConfiguration.Value.MainAdminUserId)
            {
                return;
            }

            try
            {
                await updateSettingsService.ExecuteAsync(botConfiguration.Value, cancellationToken);
                await commandMenuInitializationService.ConfigureAsync(cancellationToken);
                await SendPrivateMessageAsync(
                    message.Chat.Id,
                    "Настройки и список администраторов обновлены.",
                    cancellationToken);
                logger.LogInformation("Main admin manually synchronized bot settings and administrator cache.");
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Main admin could not synchronize bot settings and administrator cache.");
                await SendPrivateMessageAsync(
                    message.Chat.Id,
                    "Не удалось обновить настройки и список администраторов. Попробуйте ещё раз позже.",
                    cancellationToken);
            }

            return;
        }

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
                privateAdminDialogState.Start(user.Id, PrivateAdminDialogKind.ExcludeTournaments);
                await SendPrivateMessageAsync(
                    message.Chat.Id,
                    "Перечислите ID или ссылки на турниры, которые нужно исключить. Для отмены отправьте /cancel.",
                    cancellationToken);
                return;
            }

            privateAdminDialogState.Cancel(user.Id);
            await ProcessExclusionAsync(message.Chat.Id, user.Id, payload, cancellationToken);
            return;
        }

        if (await IsBotCommandAsync(messageText, BotCommands.Force, cancellationToken))
        {
            if (!commandContext.IsPrivateChat
                || !await pollCommandAuthorizer.IsAuthorizedAsync(commandContext, cancellationToken))
            {
                return;
            }

            var payload = GetCommandPayload(messageText);
            if (string.IsNullOrWhiteSpace(payload))
            {
                privateAdminDialogState.Start(user.Id, PrivateAdminDialogKind.ForceTournaments);
                await SendPrivateMessageAsync(
                    message.Chat.Id,
                    "Перечислите ID или ссылки на синхроны, которые нужно принудительно включить в подходящий опрос. Для отмены отправьте /cancel.",
                    cancellationToken);
                return;
            }

            privateAdminDialogState.Cancel(user.Id);
            await ProcessForcedTournamentsAsync(message.Chat.Id, user.Id, payload, cancellationToken);
            return;
        }

        if (await IsBotCommandAsync(messageText, BotCommands.Cancel, cancellationToken))
        {
            if (!commandContext.IsPrivateChat
                || !await pollCommandAuthorizer.IsAuthorizedAsync(commandContext, cancellationToken))
            {
                return;
            }

            var cancelledDialog = privateAdminDialogState.Cancel(user.Id);
            await SendPrivateMessageAsync(
                message.Chat.Id,
                cancelledDialog is not null ? "Диалог отменён." : "Нет активного диалога для отмены.",
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

        if (await IsBotCommandAsync(messageText, BotCommands.Stop, cancellationToken))
        {
            if (!await pollCommandAuthorizer.IsAuthorizedAsync(commandContext, cancellationToken))
            {
                return;
            }

            await stopPollService.StopActivePollAsync(cancellationToken);
            logger.LogInformation("Received /stop command.");
            return;
        }

        if (await IsBotCommandAsync(messageText, BotCommands.MakePost, cancellationToken))
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

        if (!commandContext.IsPrivateChat
            || !await pollCommandAuthorizer.IsAuthorizedAsync(commandContext, cancellationToken))
        {
            return;
        }

        switch (privateAdminDialogState.GetActive(user.Id))
        {
            case PrivateAdminDialogKind.ExcludeTournaments:
                await ProcessExclusionAsync(message.Chat.Id, user.Id, messageText, cancellationToken);
                break;
            case PrivateAdminDialogKind.ForceTournaments:
                await ProcessForcedTournamentsAsync(message.Chat.Id, user.Id, messageText, cancellationToken);
                break;
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

        privateAdminDialogState.Cancel(userId);
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

    private async Task ProcessForcedTournamentsAsync(
        long chatId,
        long userId,
        string input,
        CancellationToken cancellationToken)
    {
        ForceTournamentsResult result;
        try
        {
            result = await forceTournamentsService.ExecuteAsync(input, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(
                exception,
                "Could not validate forced tournaments for Telegram user {TelegramUserId} in chat {ChatId}.",
                userId,
                chatId);
            await SendPrivateMessageAsync(
                chatId,
                "Не удалось проверить турниры в CHGK API. Попробуйте ещё раз позже.",
                cancellationToken);
            return;
        }

        if (!result.IsValid)
        {
            var errorMessage = result.IsEmptyInput
                ? "Укажите хотя бы один ID или ссылку на турнир."
                : result.InvalidTokens.Count > 0
                    ? $"Не удалось распознать: {string.Join(", ", result.InvalidTokens)}. Укажите ID или ссылки на турниры."
                    : $"Не найдены турниры: {string.Join(", ", result.NotFoundTournamentIds)}. Исправьте список и попробуйте ещё раз.";
            await SendPrivateMessageAsync(chatId, errorMessage, cancellationToken);
            logger.LogInformation(
                "Rejected forced tournament input from Telegram user {TelegramUserId} in chat {ChatId}. Invalid token count: {InvalidTokenCount}; not found ID count: {NotFoundTournamentCount}.",
                userId,
                chatId,
                result.InvalidTokens.Count,
                result.NotFoundTournamentIds.Count);
            return;
        }

        privateAdminDialogState.Cancel(userId);
        await SendHtmlPrivateMessageAsync(chatId, ForcedTournamentResultFormatter.Format(result), cancellationToken);
        logger.LogInformation(
            "Queued forced tournaments for Telegram user {TelegramUserId} in chat {ChatId}. Added: {AddedCount}; already queued: {AlreadyQueuedCount}.",
            userId,
            chatId,
            result.AddedTournamentIds.Count,
            result.AlreadyQueuedTournamentIds.Count);
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
