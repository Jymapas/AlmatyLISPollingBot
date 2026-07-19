using AlmatyLISPollingBot.Application.Abstractions.Clock;
using AlmatyLISPollingBot.Application.Abstractions.Messaging;
using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Contracts.Polls;
using AlmatyLISPollingBot.Domain.Entities;
using AlmatyLISPollingBot.Domain.Enums;
using AlmatyLISPollingBot.Domain.Common;

namespace AlmatyLISPollingBot.Application.Features.Polls.StartPoll;

public sealed class StartPollService
{
    private readonly IClock clock;
    private readonly IForcedTournamentRepository forcedTournamentRepository;
    private readonly IPollSessionRepository pollSessionRepository;
    private readonly PollCandidatePreparationService candidatePreparationService;
    private readonly TournamentListFormatter tournamentListFormatter;
    private readonly IPollPublisher pollPublisher;
    private readonly IChatBotClient chatBotClient;

    public StartPollService(
        IClock clock,
        IForcedTournamentRepository forcedTournamentRepository,
        IPollSessionRepository pollSessionRepository,
        PollCandidatePreparationService candidatePreparationService,
        TournamentListFormatter tournamentListFormatter,
        IPollPublisher pollPublisher,
        IChatBotClient chatBotClient)
    {
        this.clock = clock;
        this.forcedTournamentRepository = forcedTournamentRepository;
        this.pollSessionRepository = pollSessionRepository;
        this.candidatePreparationService = candidatePreparationService;
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
        var preparationResult = await candidatePreparationService.PrepareAsync(request, cancellationToken);
        switch (preparationResult.RejectionReason)
        {
            case null:
                break;
            case PollCandidatePreparationRejectionReason.TargetDateAlreadyStopped:
                return new StartPollResult(null, PollStartRejectionReason.TargetDateAlreadyStopped);
            case PollCandidatePreparationRejectionReason.TooManyForcedCandidates:
                await chatBotClient.SendMainAdminAlertAsync(
                    $"Невозможно создать опрос на {preparationResult.TargetDate:dd.MM.yyyy}: доступно {preparationResult.ForcedCandidateCount} принудительно добавленных синхронов, а лимит — {PollRules.MaxTournamentOptions}.",
                    cancellationToken);
                return new StartPollResult(null, null);
            case PollCandidatePreparationRejectionReason.NoCandidates:
                await chatBotClient.SendMainAdminAlertAsync(
                    $"Не найдено подходящих синхронов для опроса на {preparationResult.TargetDate:dd.MM.yyyy}.",
                    cancellationToken);
                return new StartPollResult(null, null);
            default:
                throw new InvalidOperationException("Unsupported poll candidate preparation result.");
        }

        var settings = preparationResult.Settings;
        var targetDate = preparationResult.TargetDate;
        var stopAtUtc = preparationResult.StopAtUtc;
        var candidates = preparationResult.Candidates;
        var forcedTournamentIds = preparationResult.IncludedForcedTournamentIds;
        var formattingResult = await tournamentListFormatter.FormatAsync(
            candidates,
            TournamentIdDisplayMode.WithoutTournamentId,
            TournamentPaymentCategoriesDisplayMode.All,
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
                try
                {
                    await pollPublisher.StopPollAsync(activePoll.ChatId, activePoll.PollMessageId.Value, cancellationToken);
                }
                catch (PollNotFoundException)
                {
                }

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

        if (publishedPoll.Options is not null)
        {
            pollSession.OptionStates.AddRange(publishedPoll.Options.Select(x => new PollOptionState
            {
                PersistentId = x.PersistentId,
                Text = x.Text,
                Position = x.Position,
                IsResultsOption = x.Position == publishedPoll.Options.Count - 1,
                LastSnapshotAtUtc = clock.UtcNow
            }));
        }

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
