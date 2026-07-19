using AlmatyLISPollingBot.Application.Features.MakePost;
using AlmatyLISPollingBot.Application.Features.Administrators;
using AlmatyLISPollingBot.Application.Features.ExcludedTournaments;
using AlmatyLISPollingBot.Application.Features.ForcedTournaments;
using AlmatyLISPollingBot.Application.Features.Polls.Options;
using AlmatyLISPollingBot.Application.Features.Polls.Preview;
using AlmatyLISPollingBot.Application.Features.Polls.StartPoll;
using AlmatyLISPollingBot.Application.Features.Polls.StopPoll;
using AlmatyLISPollingBot.Application.Features.Polls.Results;
using AlmatyLISPollingBot.Application.Abstractions.Tournaments;
using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Abstractions.Clock;
using AlmatyLISPollingBot.Application.Contracts.Tournaments;
using AlmatyLISPollingBot.Application.Contracts.Bot;
using AlmatyLISPollingBot.Application.Contracts.Polls;
using AlmatyLISPollingBot.Domain.Common;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using AlmatyLISPollingBot.Worker.HostedServices;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types.ReplyMarkups;
using AlmatyLISPollingBot.Domain.Enums;
using System.Net;

namespace AlmatyLISPollingBot.Worker.Telegram;

public sealed class TelegramUpdateRouter
{
    private const int TelegramMessageMaxLength = 4096;

    private readonly StartPollService startPollService;
    private readonly PreviewPollService previewPollService;
    private readonly ListTournamentOptionsService listTournamentOptionsService;
    private readonly StopPollService stopPollService;
    private readonly MakePostService makePostService;
    private readonly ExcludeTournamentsService excludeTournamentsService;
    private readonly UnexcludeTournamentsService unexcludeTournamentsService;
    private readonly ForceTournamentsService forceTournamentsService;
    private readonly UpdateSettingsService updateSettingsService;
    private readonly PollCommandAuthorizer pollCommandAuthorizer;
    private readonly PollStateUpdateService pollStateUpdateService;
    private readonly PollResultsService pollResultsService;
    private readonly IShadowBannedUserRepository shadowBannedUserRepository;
    private readonly IClock clock;
    private readonly TelegramCommandMenuInitializationService commandMenuInitializationService;
    private readonly IOptions<BotConfiguration> botConfiguration;
    private readonly IPrivateAdminDialogState privateAdminDialogState;
    private readonly IChgkTournamentClient tournamentClient;
    private readonly ITelegramBotClient botClient;
    private readonly ILogger<TelegramUpdateRouter> logger;

    public TelegramUpdateRouter(
        StartPollService startPollService,
        PreviewPollService previewPollService,
        ListTournamentOptionsService listTournamentOptionsService,
        StopPollService stopPollService,
        MakePostService makePostService,
        ExcludeTournamentsService excludeTournamentsService,
        UnexcludeTournamentsService unexcludeTournamentsService,
        ForceTournamentsService forceTournamentsService,
        UpdateSettingsService updateSettingsService,
        PollCommandAuthorizer pollCommandAuthorizer,
        PollStateUpdateService pollStateUpdateService,
        PollResultsService pollResultsService,
        IShadowBannedUserRepository shadowBannedUserRepository,
        IClock clock,
        TelegramCommandMenuInitializationService commandMenuInitializationService,
        IOptions<BotConfiguration> botConfiguration,
        IPrivateAdminDialogState privateAdminDialogState,
        IChgkTournamentClient tournamentClient,
        ITelegramBotClient botClient,
        ILogger<TelegramUpdateRouter> logger)
    {
        this.startPollService = startPollService;
        this.previewPollService = previewPollService;
        this.listTournamentOptionsService = listTournamentOptionsService;
        this.stopPollService = stopPollService;
        this.makePostService = makePostService;
        this.excludeTournamentsService = excludeTournamentsService;
        this.unexcludeTournamentsService = unexcludeTournamentsService;
        this.forceTournamentsService = forceTournamentsService;
        this.updateSettingsService = updateSettingsService;
        this.pollCommandAuthorizer = pollCommandAuthorizer;
        this.pollStateUpdateService = pollStateUpdateService;
        this.pollResultsService = pollResultsService;
        this.shadowBannedUserRepository = shadowBannedUserRepository;
        this.clock = clock;
        this.commandMenuInitializationService = commandMenuInitializationService;
        this.botConfiguration = botConfiguration;
        this.privateAdminDialogState = privateAdminDialogState;
        this.tournamentClient = tournamentClient;
        this.botClient = botClient;
        this.logger = logger;
    }

    public async Task RouteAsync(Update update, CancellationToken cancellationToken)
    {
        if (update.Poll is not null)
        {
            await pollStateUpdateService.ApplyPollSnapshotAsync(
                new PollSnapshot(update.Poll.Id, update.Poll.Options.Select((x, index) => new PollOptionSnapshot(x.PersistentId, x.Text, index, x.VoterCount)).ToArray()),
                cancellationToken);
            return;
        }

        if (update.PollAnswer is not null)
        {
            var answer = update.PollAnswer;
            if (answer.User is not null)
            {
                await pollStateUpdateService.ApplyPollAnswerAsync(
                    new PollAnswerSnapshot(answer.PollId, PollVoterKind.User, answer.User.Id, FormatUserName(answer.User), answer.User.Username, answer.OptionPersistentIds, update.Id),
                    cancellationToken);
            }
            else if (answer.VoterChat is not null)
            {
                await pollStateUpdateService.ApplyPollAnswerAsync(
                    new PollAnswerSnapshot(answer.PollId, PollVoterKind.Chat, answer.VoterChat.Id, answer.VoterChat.Title ?? answer.VoterChat.Username ?? answer.VoterChat.Id.ToString(), null, answer.OptionPersistentIds, update.Id),
                    cancellationToken);
            }
            return;
        }

        if (update.CallbackQuery is not null)
        {
            await HandleCallbackAsync(update.CallbackQuery, cancellationToken);
            return;
        }

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

        if (await IsBotCommandAsync(messageText, BotCommands.Unexclude, cancellationToken))
        {
            if (!commandContext.IsPrivateChat
                || !await pollCommandAuthorizer.IsAuthorizedAsync(commandContext, cancellationToken))
            {
                return;
            }

            var payload = GetCommandPayload(messageText);
            if (string.IsNullOrWhiteSpace(payload))
            {
                privateAdminDialogState.Start(user.Id, PrivateAdminDialogKind.UnexcludeTournaments);
                await SendPrivateMessageAsync(
                    message.Chat.Id,
                    "Перечислите ID или ссылки на турниры, которые нужно вернуть в пул. Для отмены отправьте /cancel.",
                    cancellationToken);
                return;
            }

            privateAdminDialogState.Cancel(user.Id);
            await ProcessUnexclusionAsync(message.Chat.Id, user.Id, payload, cancellationToken);
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

            var requestParseResult = StartPollRequestParser.Parse(GetCommandPayload(messageText));
            if (!requestParseResult.IsValid)
            {
                await SendPrivateMessageAsync(
                    message.Chat.Id,
                    "Используйте: /poll, /poll 1, /poll дд.мм.гггг или /poll дд.мм.гггг 1.",
                    cancellationToken);
                return;
            }

            var result = await startPollService.StartAsync(requestParseResult.Request!, cancellationToken);
            if (result.RejectionReason == PollStartRejectionReason.TargetDateAlreadyStopped)
            {
                await SendPrivateMessageAsync(
                    message.Chat.Id,
                    "Нельзя создать опрос: время его автоматической остановки для этой даты уже прошло.",
                    cancellationToken);
                return;
            }

            logger.LogInformation(
                "Received /poll command. Target date: {TargetDate}; desired tournament count: {DesiredTournamentCount}.",
                requestParseResult.Request!.TargetDate,
                requestParseResult.Request.DesiredTournamentCount);
            return;
        }

        if (await IsBotCommandAsync(messageText, BotCommands.Preview, cancellationToken))
        {
            if (!commandContext.IsPrivateChat
                || !await pollCommandAuthorizer.IsAuthorizedAsync(commandContext, cancellationToken))
            {
                return;
            }

            var requestParseResult = StartPollRequestParser.Parse(GetCommandPayload(messageText));
            if (!requestParseResult.IsValid)
            {
                await SendPrivateMessageAsync(
                    message.Chat.Id,
                    "Используйте: /preview, /preview 1, /preview дд.мм.гггг или /preview дд.мм.гггг 1.",
                    cancellationToken);
                return;
            }

            PollPreviewResult result;
            try
            {
                result = await previewPollService.ExecuteAsync(requestParseResult.Request!, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(
                    exception,
                    "Could not create poll preview for Telegram user {TelegramUserId} in chat {ChatId}.",
                    user.Id,
                    message.Chat.Id);
                await SendPrivateMessageAsync(
                    message.Chat.Id,
                    "Не удалось сформировать предпросмотр опроса. Попробуйте ещё раз позже.",
                    cancellationToken);
                return;
            }

            switch (result.RejectionReason)
            {
                case null:
                    break;
                case PollCandidatePreparationRejectionReason.TargetDateAlreadyStopped:
                    await SendPrivateMessageAsync(
                        message.Chat.Id,
                        "Нельзя сформировать предпросмотр: время автоматической остановки для этой даты уже прошло.",
                        cancellationToken);
                    return;
                case PollCandidatePreparationRejectionReason.TooManyForcedCandidates:
                    await SendPrivateMessageAsync(
                        message.Chat.Id,
                        $"Невозможно сформировать опрос на {result.TargetDate:dd.MM.yyyy}: доступно {result.ForcedCandidateCount} принудительно добавленных синхронов, а лимит — {PollRules.MaxTournamentOptions}.",
                        cancellationToken);
                    return;
                case PollCandidatePreparationRejectionReason.NoCandidates:
                    await SendPrivateMessageAsync(
                        message.Chat.Id,
                        $"Не найдено подходящих синхронов для опроса на {result.TargetDate:dd.MM.yyyy}.",
                        cancellationToken);
                    return;
                default:
                    logger.LogError(
                        "Unsupported poll preview rejection reason {RejectionReason} for Telegram user {TelegramUserId} in chat {ChatId}.",
                        result.RejectionReason,
                        user.Id,
                        message.Chat.Id);
                    await SendPrivateMessageAsync(
                        message.Chat.Id,
                        "Не удалось сформировать предпросмотр опроса. Попробуйте ещё раз позже.",
                        cancellationToken);
                    return;
            }

            await SendPrivateMessageAsync(
                message.Chat.Id,
                $"Предпросмотр опроса на {result.TargetDate:dd.MM.yyyy} (состояние на сейчас). В Telegram poll также будет вариант «посмотреть результаты».",
                cancellationToken);
            foreach (var page in result.Pages)
            {
                await SendHtmlPrivateMessageAsync(message.Chat.Id, page, cancellationToken);
            }

            logger.LogInformation(
                "Created poll preview for Telegram user {TelegramUserId} in chat {ChatId}. Target date: {TargetDate}; candidate page count: {PageCount}.",
                user.Id,
                message.Chat.Id,
                result.TargetDate,
                result.Pages.Count);
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

        if (await IsBotCommandAsync(messageText, BotCommands.Results, cancellationToken))
        {
            if (!commandContext.IsPrivateChat || !await pollCommandAuthorizer.IsAuthorizedAsync(commandContext, cancellationToken))
            {
                return;
            }

            await SendResultsAsync(message.Chat.Id, cancellationToken);
            return;
        }

        if (await IsBotCommandAsync(messageText, BotCommands.Excluded, cancellationToken))
        {
            if (!commandContext.IsPrivateChat
                || !await pollCommandAuthorizer.IsAuthorizedAsync(commandContext, cancellationToken))
            {
                return;
            }

            TournamentOptionsResult result;
            try
            {
                result = await listTournamentOptionsService.ExecuteExcludedAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(
                    exception,
                    "Could not list excluded tournaments for Telegram user {TelegramUserId} in chat {ChatId}.",
                    user.Id,
                    message.Chat.Id);
                await SendPrivateMessageAsync(
                    message.Chat.Id,
                    "Не удалось загрузить исключённые турниры. Попробуйте ещё раз позже.",
                    cancellationToken);
                return;
            }

            if (result.Pages.Count == 0)
            {
                await SendPrivateMessageAsync(
                    message.Chat.Id,
                    $"Не найдено исключённых турниров на {result.TargetDate:dd.MM.yyyy}.",
                    cancellationToken);
            }
            else
            {
                foreach (var page in result.Pages)
                {
                    await SendHtmlPrivateMessageAsync(message.Chat.Id, page, cancellationToken);
                }
            }

            logger.LogInformation(
                "Listed {PageCount} pages of excluded tournaments for Telegram user {TelegramUserId} in chat {ChatId}.",
                result.Pages.Count,
                user.Id,
                message.Chat.Id);
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
            case PrivateAdminDialogKind.UnexcludeTournaments:
                await ProcessUnexclusionAsync(message.Chat.Id, user.Id, messageText, cancellationToken);
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

    private async Task ProcessUnexclusionAsync(
        long chatId,
        long userId,
        string input,
        CancellationToken cancellationToken)
    {
        var result = await unexcludeTournamentsService.ExecuteAsync(input, cancellationToken);
        if (!result.IsValid)
        {
            var errorMessage = result.IsEmptyInput
                ? "Укажите хотя бы один ID или ссылку на турнир."
                : $"Не удалось распознать: {string.Join(", ", result.InvalidTokens)}. Укажите ID или ссылки на турниры.";
            await SendPrivateMessageAsync(chatId, errorMessage, cancellationToken);
            logger.LogInformation(
                "Rejected tournament return input from Telegram user {TelegramUserId} in chat {ChatId}. Invalid token count: {InvalidTokenCount}.",
                userId,
                chatId,
                result.InvalidTokens.Count);
            return;
        }

        privateAdminDialogState.Cancel(userId);
        var tournamentIds = result.ReturnedTournamentIds
            .Concat(result.AlreadyIncludedTournamentIds)
            .ToArray();
        var tournaments = await GetTournamentDetailsAsync(tournamentIds, cancellationToken);
        await SendHtmlPrivateMessageAsync(
            chatId,
            UnexcludeTournamentsResultFormatter.Format(result, tournaments),
            cancellationToken);
        logger.LogInformation(
            "Returned tournaments to the pool for Telegram user {TelegramUserId} in chat {ChatId}. Returned: {ReturnedCount}; already included: {AlreadyIncludedCount}.",
            userId,
            chatId,
            result.ReturnedTournamentIds.Count,
            result.AlreadyIncludedTournamentIds.Count);
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

    private async Task SendResultsAsync(long chatId, CancellationToken cancellationToken)
    {
        var summary = await pollResultsService.GetActiveAsync(cancellationToken);
        if (summary is null)
        {
            await SendPrivateMessageAsync(chatId, "Нет активного опроса.", cancellationToken);
            return;
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(botConfiguration.Value.ApplicationTimeZone);
        var pages = SplitTelegramMessage(PollResultsService.FormatSummary(summary, timeZone));
        var keyboard = new InlineKeyboardMarkup(summary.Options
            .Select(x => new[] { InlineKeyboardButton.WithCallbackData(x.Text.Length > 60 ? string.Concat(x.Text.AsSpan(0, 59), "…") : x.Text, $"r|{ToCallbackToken(summary.PollSessionId)}|{ToCallbackToken(x.OptionId)}") }));
        for (var index = 0; index < pages.Count; index++)
        {
            await botClient.SendMessage(chatId, pages[index], parseMode: ParseMode.Html, replyMarkup: index == pages.Count - 1 ? keyboard : null, cancellationToken: cancellationToken);
        }
    }

    private async Task HandleCallbackAsync(CallbackQuery callback, CancellationToken cancellationToken)
    {
        try
        {
            var message = callback.Message;
            if (message is null || message.Chat.Type != ChatType.Private || callback.From is null
                || !await pollCommandAuthorizer.IsAuthorizedAsync(new PollCommandContext(message.Chat.Id, callback.From.Id, true), cancellationToken))
            {
                return;
            }

            var tokens = (callback.Data ?? string.Empty).Split('|');
            if (tokens.Length == 3 && tokens[0] == "r" && TryParseGuidToken(tokens[1], out var sessionId) && TryParseGuidToken(tokens[2], out var optionId))
            {
                await SendVotersAsync(message.Chat.Id, sessionId, optionId, cancellationToken);
                return;
            }

            if (tokens.Length == 5 && tokens[0] == "b" && TryParseGuidToken(tokens[1], out var banSessionId) && TryParseGuidToken(tokens[2], out var banOptionId) && TryParseLongToken(tokens[3], out var voterId) && (tokens[4] == "0" || tokens[4] == "1"))
            {
                var excluding = tokens[4] == "1";
                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("Подтвердить", $"c|{ToCallbackToken(banSessionId)}|{ToCallbackToken(banOptionId)}|{ToCallbackToken(voterId)}|{tokens[4]}") },
                    new[] { InlineKeyboardButton.WithCallbackData("Отмена", "n|0|0|0|0") }
                });
                await botClient.SendMessage(message.Chat.Id, excluding ? "Исключить пользователя из учёта результатов?" : "Вернуть пользователя в учёт результатов?", replyMarkup: keyboard, cancellationToken: cancellationToken);
                return;
            }

            if (tokens.Length == 5 && tokens[0] == "c" && TryParseGuidToken(tokens[1], out var confirmSessionId) && TryParseGuidToken(tokens[2], out var confirmOptionId) && TryParseLongToken(tokens[3], out var targetUserId) && (tokens[4] == "0" || tokens[4] == "1"))
            {
                var voters = await pollResultsService.GetVotersAsync(confirmSessionId, confirmOptionId, cancellationToken);
                if (voters is null || !voters.Any(x => x.VoterKind == PollVoterKind.User && x.TelegramPeerId == targetUserId))
                {
                    return;
                }

                if (tokens[4] == "1")
                {
                    await shadowBannedUserRepository.SetExcludedAsync(targetUserId, callback.From.Id, clock.UtcNow, cancellationToken);
                }
                else
                {
                    await shadowBannedUserRepository.SetIncludedAsync(targetUserId, callback.From.Id, clock.UtcNow, cancellationToken);
                }
            }
        }
        finally
        {
            await botClient.AnswerCallbackQuery(callback.Id, cancellationToken: cancellationToken);
        }
    }

    private async Task SendVotersAsync(long chatId, Guid sessionId, Guid optionId, CancellationToken cancellationToken)
    {
        var voters = await pollResultsService.GetVotersAsync(sessionId, optionId, cancellationToken);
        if (voters is null)
        {
            return;
        }

        var lines = new List<string> { "<b>Голосовавшие</b>" };
        var buttons = new List<IEnumerable<InlineKeyboardButton>>();
        foreach (var voter in voters)
        {
            var name = WebUtility.HtmlEncode(voter.DisplayName);
            var identity = voter.VoterKind == PollVoterKind.User ? $"<a href=\"tg://user?id={voter.TelegramPeerId}\">{name}</a>" : name;
            var username = string.IsNullOrWhiteSpace(voter.Username) ? string.Empty : $" @{WebUtility.HtmlEncode(voter.Username)}";
            lines.Add($"{identity}{username}{(voter.IsExcluded ? " 🚫 не учитывается" : string.Empty)}");
            if (voter.VoterKind == PollVoterKind.User)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(voter.IsExcluded ? "Вернуть в учёт" : "Исключить", $"b|{ToCallbackToken(sessionId)}|{ToCallbackToken(optionId)}|{ToCallbackToken(voter.TelegramPeerId)}|{(voter.IsExcluded ? 0 : 1)}") });
            }
        }

        foreach (var page in SplitTelegramMessage(string.Join('\n', lines)))
        {
            await botClient.SendMessage(chatId, page, parseMode: ParseMode.Html, cancellationToken: cancellationToken);
        }
        if (buttons.Count > 0)
        {
            await botClient.SendMessage(chatId, "Действия:", replyMarkup: new InlineKeyboardMarkup(buttons), cancellationToken: cancellationToken);
        }
    }

    private static IReadOnlyList<string> SplitTelegramMessage(string message)
    {
        var pages = new List<string>();
        var current = new System.Text.StringBuilder();
        foreach (var line in message.Split('\n'))
        {
            if (current.Length > 0 && current.Length + line.Length + 1 > TelegramMessageMaxLength)
            {
                pages.Add(current.ToString());
                current.Clear();
            }
            if (line.Length > TelegramMessageMaxLength)
            {
                pages.Add(line[..TelegramMessageMaxLength]);
                continue;
            }
            if (current.Length > 0) current.Append('\n');
            current.Append(line);
        }
        if (current.Length > 0) pages.Add(current.ToString());
        return pages;
    }

    private static string FormatUserName(User user) => string.Join(' ', new[] { user.FirstName, user.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private static string ToCallbackToken(Guid value) => Convert.ToBase64String(value.ToByteArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string ToCallbackToken(long value) => Convert.ToBase64String(BitConverter.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static bool TryParseGuidToken(string value, out Guid result)
    {
        result = Guid.Empty;
        if (value.Length != 22) return false;
        try
        {
            var bytes = Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + "==");
            if (bytes.Length != 16) return false;
            result = new Guid(bytes);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TryParseLongToken(string value, out long result)
    {
        result = 0;
        if (value.Length != 11) return false;
        try
        {
            var bytes = Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + "=");
            if (bytes.Length != sizeof(long)) return false;
            result = BitConverter.ToInt64(bytes, 0);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
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
                "Could not load tournament titles for {TournamentCount} tournaments.",
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
