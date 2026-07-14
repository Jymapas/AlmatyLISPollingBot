using AlmatyLISPollingBot.Application.Contracts.Tournaments;
using AlmatyLISPollingBot.Domain.Common;

namespace AlmatyLISPollingBot.Application.Features.Polls.StartPoll;

public sealed class PollCandidateSelectionService
{
    public IReadOnlyList<PollTournamentCandidate> SelectCandidates(
        IEnumerable<TournamentDetails> tournaments,
        DateOnly targetDate,
        IReadOnlyCollection<int>? excludedTournamentIds = null)
    {
        return SelectCandidates(
            tournaments,
            targetDate,
            excludedTournamentIds,
            includeExcluded: false,
            maximumCandidateCount: PollRules.MaxTournamentOptions);
    }

    public IReadOnlyList<PollTournamentCandidate> SelectAllCandidates(
        IEnumerable<TournamentDetails> tournaments,
        DateOnly targetDate,
        IReadOnlyCollection<int>? excludedTournamentIds = null)
    {
        return SelectCandidates(
            tournaments,
            targetDate,
            excludedTournamentIds,
            includeExcluded: true,
            maximumCandidateCount: null);
    }

    private static IReadOnlyList<PollTournamentCandidate> SelectCandidates(
        IEnumerable<TournamentDetails> tournaments,
        DateOnly targetDate,
        IReadOnlyCollection<int>? excludedTournamentIds,
        bool includeExcluded,
        int? maximumCandidateCount)
    {
        ArgumentNullException.ThrowIfNull(tournaments);

        var excludedIds = excludedTournamentIds is null
            ? new HashSet<int>()
            : new HashSet<int>(excludedTournamentIds);
        var firstSlot = PollRules.GetSlotStart(targetDate, PollRules.FirstSlotTime);
        var secondSlot = PollRules.GetSlotStart(targetDate, PollRules.SecondSlotTime);

        IEnumerable<CandidateAvailability> candidates = tournaments
            .Where(x => PollRules.IsSupportedTournamentType(x.TypeId))
            .Where(x => x.HasRussianLanguage)
            .Where(x => x.HasChgkGgRating)
            .Where(x => includeExcluded || !excludedIds.Contains(x.Id))
            .Select(x => new CandidateAvailability(
                x,
                PollRules.IsAvailableAtSlot(x.DateStart, x.DateEnd, firstSlot),
                PollRules.IsAvailableAtSlot(x.DateStart, x.DateEnd, secondSlot)))
            .Where(x => x.IsAvailableAtFirstSlot || x.IsAvailableAtSecondSlot)
            .OrderByDescending(x => x.Tournament.DifficultyForecast.HasValue)
            .ThenByDescending(x => x.Tournament.DifficultyForecast)
            .ThenBy(x => x.Tournament.Title, StringComparer.Ordinal)
            .ThenBy(x => x.Tournament.Id);

        if (maximumCandidateCount is not null)
        {
            candidates = candidates.Take(maximumCandidateCount.Value);
        }

        return candidates
            .Select((x, index) => new PollTournamentCandidate(
                x.Tournament,
                x.IsAvailableAtFirstSlot,
                x.IsAvailableAtSecondSlot,
                index)
            {
                IsExcluded = excludedIds.Contains(x.Tournament.Id)
            })
            .ToArray();
    }

    private sealed record CandidateAvailability(
        TournamentDetails Tournament,
        bool IsAvailableAtFirstSlot,
        bool IsAvailableAtSecondSlot);
}
