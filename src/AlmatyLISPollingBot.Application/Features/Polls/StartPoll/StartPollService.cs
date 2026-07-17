using AlmatyLISPollingBot.Application.Abstractions.Clock;
using AlmatyLISPollingBot.Application.Abstractions.Messaging;
using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Abstractions.Tournaments;
using AlmatyLISPollingBot.Application.Contracts.Polls;
using AlmatyLISPollingBot.Application.Features.Common;
using AlmatyLISPollingBot.Domain.Entities;
using AlmatyLISPollingBot.Domain.Enums;
using AlmatyLISPollingBot.Domain.Common;

namespace AlmatyLISPollingBot.Application.Features.Polls.StartPoll;

public sealed class StartPollService
{
    private readonly IClock clock;
    private readonly IBotSettingsRepository settingsRepository;
    private readonly IReadOnlyLookupRepository lookupRepository;
    private readonly IForcedTournamentRepository forcedTournamentRepository;
    private readonly IPollSessionRepository pollSessionRepository;
    private readonly IChgkTournamentClient tournamentClient;
    private readonly PollCandidateSelectionService candidateSelectionService;
    private readonly TournamentListFormatter tournamentListFormatter;
    private readonly IPollPublisher pollPublisher;
    private readonly IChatBotClient chatBotClient;

    public StartPollService(
        IClock clock,
        IBotSettingsRepository settingsRepository,
        IReadOnlyLookupRepository lookupRepository,
        IForcedTournamentRepository forcedTournamentRepository,
        IPollSessionRepository pollSessionRepository,
        IChgkTournamentClient tournamentClient,
        PollCandidateSelectionService candidateSelectionService,
        TournamentListFormatter tournamentListFormatter,
        IPollPublisher pollPublisher,
        IChatBotClient chatBotClient)
    {
        this.clock = clock;
        this.settingsRepository = settingsRepository;
        this.lookupRepository = lookupRepository;
        this.forcedTournamentRepository = forcedTournamentRepository;
        this.pollSessionRepository = pollSessionRepository;
        this.tournamentClient = tournamentClient;
        this.candidateSelectionService = candidateSelectionService;
        this.tournamentListFormatter = tournamentListFormatter;
        this.pollPublisher = pollPublisher;
        this.chatBotClient = chatBotClient;
    }

    public async Task<PollSession?> StartAsync(CancellationToken cancellationToken)
    {
        return (await StartAsync(StartPollRequest.Default, cancellationToken)).PollSession;
    }

    public async Task<StartPollResult> StartAsync(
        StartPollRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!PollRules.IsSupportedDesiredTournamentCount(request.DesiredTournamentCount))
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.DesiredTournamentCount,
                "Only one or two desired tournaments are supported.");
        }

        var settings = await settingsRepository.GetAsync(cancellationToken)
            ?? throw new InvalidOperationException("Bot settings are not initialized.");

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.ApplicationTimeZone);
        var targetDate = request.TargetDate ?? TargetDateCalculator.GetNextSaturday(clock.UtcNow, timeZone);
        var stopAtUtc = PollRules.GetPollStopAt(targetDate, settings.DefaultPollStopTime).ToUniversalTime();
        if (stopAtUtc <= clock.UtcNow)
        {
            return new StartPollResult(null, PollStartRejectionReason.TargetDateAlreadyStopped);
        }

        var excludedIdsTask = lookupRepository.GetExcludedTournamentIdsAsync(cancellationToken);
        var forcedTournamentsTask = forcedTournamentRepository.GetQueuedAsync(cancellationToken);
        var tournamentsTask = tournamentClient.GetTournamentsIntersectingDateAsync(targetDate, cancellationToken);
        await Task.WhenAll(excludedIdsTask, forcedTournamentsTask, tournamentsTask);
        var excludedIds = await excludedIdsTask;
        var forcedTournaments = await forcedTournamentsTask;
        var tournaments = await tournamentsTask;

        var forcedCandidates = candidateSelectionService.SelectForcedCandidates(
            tournaments,
            targetDate,
            forcedTournaments.Select(x => x.TournamentId).ToArray());
        if (forcedCandidates.Count > PollRules.MaxTournamentOptions)
        {
            await chatBotClient.SendMainAdminAlertAsync(
                $"Невозможно создать опрос на {targetDate:dd.MM.yyyy}: доступно {forcedCandidates.Count} принудительно добавленных синхронов, а лимит — {PollRules.MaxTournamentOptions}.",
                cancellationToken);
            return new StartPollResult(null, null);
        }

        var forcedTournamentIds = forcedCandidates.Select(x => x.Tournament.Id).ToHashSet();
        var regularCandidates = candidateSelectionService.SelectCandidates(
            tournaments,
            targetDate,
            excludedIds)
            .Where(x => !forcedTournamentIds.Contains(x.Tournament.Id))
            .Take(PollRules.MaxTournamentOptions - forcedCandidates.Count);
        var candidates = forcedCandidates
            .Concat(regularCandidates)
            .Select((candidate, index) => candidate with { SortOrder = index })
            .ToArray();
        if (candidates.Length == 0)
        {
            await chatBotClient.SendMainAdminAlertAsync(
                $"Не найдено подходящих синхронов для опроса на {targetDate:dd.MM.yyyy}.",
                cancellationToken);
            return new StartPollResult(null, null);
        }

        var formattingResult = await tournamentListFormatter.FormatAsync(
            candidates,
            TournamentIdDisplayMode.WithoutTournamentId,
            cancellationToken);
        var publicationRequest = CreatePublicationRequest(
            settings.TargetChatId,
            targetDate,
            request.TargetDate is not null,
            request.DesiredTournamentCount,
            candidates,
            stopAtUtc);
        var listMessageIds = new List<int>(formattingResult.Pages.Count);
        PublishedPoll? publishedPoll = null;

        try
        {
            foreach (var page in formattingResult.Pages)
            {
                listMessageIds.Add(await pollPublisher.SendHtmlMessageAsync(settings.TargetChatId, page, cancellationToken));
            }

            publishedPoll = await pollPublisher.SendPollAsync(publicationRequest, cancellationToken);
            var activePoll = await pollSessionRepository.GetActiveAsync(cancellationToken);
            if (activePoll?.PollMessageId is not null)
            {
                await pollPublisher.StopPollAsync(activePoll.ChatId, activePoll.PollMessageId.Value, cancellationToken);
                activePoll.Status = PollLifecycleStatus.Stopped;
                activePoll.StoppedAtUtc = clock.UtcNow;
            }

            var pollSession = CreatePollSession(
                settings.TargetChatId,
                targetDate,
                stopAtUtc,
                listMessageIds,
                publishedPoll,
                candidates,
                request.DesiredTournamentCount);
            await pollSessionRepository.AddAsync(pollSession, cancellationToken);
            await forcedTournamentRepository.RemoveAsync(forcedTournamentIds, cancellationToken);
            await pollSessionRepository.SaveChangesAsync(cancellationToken);

            if (formattingResult.HasUnconvertedPrices)
            {
                await chatBotClient.SendMainAdminAlertAsync(
                    $"Для опроса на {targetDate:dd.MM.yyyy} не удалось получить курс к тенге для части цен.",
                    cancellationToken);
            }

            return new StartPollResult(pollSession, null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await RollbackPublishedMessagesAsync(settings.TargetChatId, listMessageIds, publishedPoll, cancellationToken);
            await chatBotClient.SendMainAdminAlertAsync(
                $"Не удалось опубликовать опрос на {targetDate:dd.MM.yyyy}.",
                cancellationToken);
            throw;
        }
    }

    private static PollPublicationRequest CreatePublicationRequest(
        long chatId,
        DateOnly targetDate,
        bool hasExplicitTargetDate,
        int desiredTournamentCount,
        IReadOnlyList<PollTournamentCandidate> candidates,
        DateTimeOffset stopAtUtc)
    {
        var options = candidates
            .Select(x => TruncatePollOption(TournamentTitleNormalizer.Normalize(x.Tournament.Title)))
            .Append(PollRules.ResultsOptionTitle)
            .ToArray();

        return new PollPublicationRequest(
            chatId,
            CreatePollQuestion(targetDate, hasExplicitTargetDate, desiredTournamentCount),
            options,
            stopAtUtc,
            IsAnonymous: false,
            AllowsMultipleAnswers: PollRules.AllowsMultipleAnswers(desiredTournamentCount),
            ShuffleOptions: false,
            AllowAddingOptions: candidates.Count < PollRules.MaxTournamentOptions);
    }

    private PollSession CreatePollSession(
        long chatId,
        DateOnly targetDate,
        DateTimeOffset stopAtUtc,
        IReadOnlyList<int> listMessageIds,
        PublishedPoll publishedPoll,
        IReadOnlyList<PollTournamentCandidate> candidates,
        int desiredTournamentCount)
    {
        var pollSession = new PollSession
        {
            ChatId = chatId,
            TargetDate = targetDate,
            DesiredTournamentCount = desiredTournamentCount,
            ScheduledStopAtUtc = stopAtUtc,
            StartedAtUtc = clock.UtcNow,
            Status = PollLifecycleStatus.Active,
            ListMessageId = listMessageIds.FirstOrDefault(),
            TelegramPollId = publishedPoll.TelegramPollId,
            PollMessageId = publishedPoll.MessageId
        };

        pollSession.Candidates.AddRange(candidates.Select(x => new PollCandidate
        {
            TournamentId = x.Tournament.Id,
            Title = x.Tournament.Title,
            DifficultyForecast = x.Tournament.DifficultyForecast,
            IsAvailableAtFirstSlot = x.IsAvailableAtFirstSlot,
            IsAvailableAtSecondSlot = x.IsAvailableAtSecondSlot,
            SortOrder = x.SortOrder
        }));

        return pollSession;
    }

    private static string CreatePollQuestion(
        DateOnly targetDate,
        bool hasExplicitTargetDate,
        int desiredTournamentCount)
    {
        var tournamentLabel = desiredTournamentCount == PollRules.SingleTournamentCount
            ? "1 синхрон"
            : "2 синхрона";
        var targetDateLabel = hasExplicitTargetDate
            ? $"на {targetDate:dd.MM.yyyy}"
            : $"на субботу, {targetDate:dd.MM.yyyy}";

        return $"Выбираем {tournamentLabel} {targetDateLabel}:";
    }

    private async Task RollbackPublishedMessagesAsync(
        long chatId,
        IReadOnlyList<int> listMessageIds,
        PublishedPoll? publishedPoll,
        CancellationToken cancellationToken)
    {
        if (publishedPoll is not null)
        {
            try
            {
                await pollPublisher.StopPollAsync(chatId, publishedPoll.MessageId, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
            }
        }

        foreach (var messageId in listMessageIds.Reverse())
        {
            try
            {
                await pollPublisher.DeleteMessageAsync(chatId, messageId, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
            }
        }
    }

    private static string TruncatePollOption(string title)
    {
        const int maxLength = 100;
        return title.Length <= maxLength
            ? title
            : string.Concat(title.AsSpan(0, maxLength - 1), "…");
    }
}
