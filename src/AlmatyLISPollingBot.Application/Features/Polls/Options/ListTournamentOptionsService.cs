using AlmatyLISPollingBot.Application.Abstractions.Clock;
using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Abstractions.Tournaments;
using AlmatyLISPollingBot.Application.Features.Common;
using AlmatyLISPollingBot.Application.Features.Polls.StartPoll;

namespace AlmatyLISPollingBot.Application.Features.Polls.Options;

public sealed class ListTournamentOptionsService
{
    private readonly IClock clock;
    private readonly IBotSettingsRepository settingsRepository;
    private readonly IReadOnlyLookupRepository lookupRepository;
    private readonly IChgkTournamentClient tournamentClient;
    private readonly PollCandidateSelectionService candidateSelectionService;
    private readonly TournamentListFormatter tournamentListFormatter;

    public ListTournamentOptionsService(
        IClock clock,
        IBotSettingsRepository settingsRepository,
        IReadOnlyLookupRepository lookupRepository,
        IChgkTournamentClient tournamentClient,
        PollCandidateSelectionService candidateSelectionService,
        TournamentListFormatter tournamentListFormatter)
    {
        this.clock = clock;
        this.settingsRepository = settingsRepository;
        this.lookupRepository = lookupRepository;
        this.tournamentClient = tournamentClient;
        this.candidateSelectionService = candidateSelectionService;
        this.tournamentListFormatter = tournamentListFormatter;
    }

    public async Task<TournamentOptionsResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        return await ExecuteAsync(onlyExcluded: false, cancellationToken);
    }

    public async Task<TournamentOptionsResult> ExecuteExcludedAsync(CancellationToken cancellationToken)
    {
        return await ExecuteAsync(onlyExcluded: true, cancellationToken);
    }

    private async Task<TournamentOptionsResult> ExecuteAsync(
        bool onlyExcluded,
        CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken)
            ?? throw new InvalidOperationException("Bot settings are not initialized.");

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.ApplicationTimeZone);
        var targetDate = TargetDateCalculator.GetNextSaturday(clock.UtcNow, timeZone);
        var excludedIdsTask = lookupRepository.GetExcludedTournamentIdsAsync(cancellationToken);
        var tournamentsTask = tournamentClient.GetTournamentsIntersectingDateAsync(targetDate, cancellationToken);
        await Task.WhenAll(excludedIdsTask, tournamentsTask);

        var candidates = candidateSelectionService.SelectAllCandidates(
            await tournamentsTask,
            targetDate,
            await excludedIdsTask);

        if (onlyExcluded)
        {
            candidates = candidates.Where(x => x.IsExcluded).ToArray();
        }

        var formattingResult = await tournamentListFormatter.FormatAsync(candidates, cancellationToken);

        return new TournamentOptionsResult(targetDate, formattingResult.Pages);
    }
}
