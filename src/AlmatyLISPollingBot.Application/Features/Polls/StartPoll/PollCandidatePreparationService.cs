using AlmatyLISPollingBot.Application.Abstractions.Clock;
using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Abstractions.Tournaments;
using AlmatyLISPollingBot.Application.Features.Common;
using AlmatyLISPollingBot.Domain.Common;

namespace AlmatyLISPollingBot.Application.Features.Polls.StartPoll;

public sealed class PollCandidatePreparationService
{
    private readonly IClock clock;
    private readonly IBotSettingsRepository settingsRepository;
    private readonly IReadOnlyLookupRepository lookupRepository;
    private readonly IForcedTournamentRepository forcedTournamentRepository;
    private readonly IChgkTournamentClient tournamentClient;
    private readonly PollCandidateSelectionService candidateSelectionService;

    public PollCandidatePreparationService(
        IClock clock,
        IBotSettingsRepository settingsRepository,
        IReadOnlyLookupRepository lookupRepository,
        IForcedTournamentRepository forcedTournamentRepository,
        IChgkTournamentClient tournamentClient,
        PollCandidateSelectionService candidateSelectionService)
    {
        this.clock = clock;
        this.settingsRepository = settingsRepository;
        this.lookupRepository = lookupRepository;
        this.forcedTournamentRepository = forcedTournamentRepository;
        this.tournamentClient = tournamentClient;
        this.candidateSelectionService = candidateSelectionService;
    }

    public async Task<PollCandidatePreparationResult> PrepareAsync(
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
            return new PollCandidatePreparationResult(
                settings,
                targetDate,
                stopAtUtc,
                Array.Empty<PollTournamentCandidate>(),
                Array.Empty<int>(),
                PollCandidatePreparationRejectionReason.TargetDateAlreadyStopped,
                ForcedCandidateCount: 0);
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
            return new PollCandidatePreparationResult(
                settings,
                targetDate,
                stopAtUtc,
                Array.Empty<PollTournamentCandidate>(),
                Array.Empty<int>(),
                PollCandidatePreparationRejectionReason.TooManyForcedCandidates,
                forcedCandidates.Count);
        }

        var includedForcedTournamentIds = forcedCandidates.Select(x => x.Tournament.Id).ToHashSet();
        var regularCandidates = candidateSelectionService.SelectCandidates(
            tournaments,
            targetDate,
            excludedIds)
            .Where(x => !includedForcedTournamentIds.Contains(x.Tournament.Id))
            .Take(PollRules.MaxTournamentOptions - forcedCandidates.Count);
        var candidates = forcedCandidates
            .Concat(regularCandidates)
            .Select((candidate, index) => candidate with { SortOrder = index })
            .ToArray();
        if (candidates.Length == 0)
        {
            return new PollCandidatePreparationResult(
                settings,
                targetDate,
                stopAtUtc,
                candidates,
                includedForcedTournamentIds,
                PollCandidatePreparationRejectionReason.NoCandidates,
                forcedCandidates.Count);
        }

        return new PollCandidatePreparationResult(
            settings,
            targetDate,
            stopAtUtc,
            candidates,
            includedForcedTournamentIds,
            RejectionReason: null,
            forcedCandidates.Count);
    }
}
