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
    private const string PollQuestionFormat = "Выбираем 2 синхрона на субботу, {0:dd.MM.yyyy}:";

    private readonly IClock clock;
    private readonly IBotSettingsRepository settingsRepository;
    private readonly IReadOnlyLookupRepository lookupRepository;
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
        this.pollSessionRepository = pollSessionRepository;
        this.tournamentClient = tournamentClient;
        this.candidateSelectionService = candidateSelectionService;
        this.tournamentListFormatter = tournamentListFormatter;
        this.pollPublisher = pollPublisher;
        this.chatBotClient = chatBotClient;
    }

    public async Task<PollSession?> StartAsync(CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken)
            ?? throw new InvalidOperationException("Bot settings are not initialized.");

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.ApplicationTimeZone);
        var targetDate = TargetDateCalculator.GetNextSaturday(clock.UtcNow, timeZone);
        var excludedIdsTask = lookupRepository.GetExcludedTournamentIdsAsync(cancellationToken);
        var tournamentsTask = tournamentClient.GetTournamentsIntersectingDateAsync(targetDate, cancellationToken);
        await Task.WhenAll(excludedIdsTask, tournamentsTask);
        var excludedIds = await excludedIdsTask;
        var tournaments = await tournamentsTask;

        var candidates = candidateSelectionService.SelectCandidates(
            tournaments,
            targetDate,
            excludedIds);
        if (candidates.Count == 0)
        {
            await chatBotClient.SendMainAdminAlertAsync(
                $"Не найдено подходящих синхронов для опроса на {targetDate:dd.MM.yyyy}.",
                cancellationToken);
            return null;
        }

        var formattingResult = await tournamentListFormatter.FormatAsync(candidates, cancellationToken);
        var stopAtUtc = PollRules.GetPollStopAt(targetDate, settings.DefaultPollStopTime).ToUniversalTime();
        var publicationRequest = CreatePublicationRequest(settings.TargetChatId, targetDate, candidates, stopAtUtc);
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
                candidates);
            await pollSessionRepository.AddAsync(pollSession, cancellationToken);
            await pollSessionRepository.SaveChangesAsync(cancellationToken);

            if (formattingResult.HasUnconvertedPrices)
            {
                await chatBotClient.SendMainAdminAlertAsync(
                    $"Для опроса на {targetDate:dd.MM.yyyy} не удалось получить курс к тенге для части цен.",
                    cancellationToken);
            }

            return pollSession;
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
        IReadOnlyList<PollTournamentCandidate> candidates,
        DateTimeOffset stopAtUtc)
    {
        var options = candidates
            .Select(x => TruncatePollOption(x.Tournament.Title))
            .Append(PollRules.ResultsOptionTitle)
            .ToArray();

        return new PollPublicationRequest(
            chatId,
            string.Format(PollQuestionFormat, targetDate),
            options,
            stopAtUtc,
            IsAnonymous: false,
            AllowsMultipleAnswers: true,
            ShuffleOptions: false,
            AllowAddingOptions: candidates.Count < PollRules.MaxTournamentOptions);
    }

    private PollSession CreatePollSession(
        long chatId,
        DateOnly targetDate,
        DateTimeOffset stopAtUtc,
        IReadOnlyList<int> listMessageIds,
        PublishedPoll publishedPoll,
        IReadOnlyList<PollTournamentCandidate> candidates)
    {
        var pollSession = new PollSession
        {
            ChatId = chatId,
            TargetDate = targetDate,
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
