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
        ArgumentNullException.ThrowIfNull(tournaments);

        var excludedIds = excludedTournamentIds is null
            ? new HashSet<int>()
            : new HashSet<int>(excludedTournamentIds);
        var firstSlot = PollRules.GetSlotStart(targetDate, PollRules.FirstSlotTime);
        var secondSlot = PollRules.GetSlotStart(targetDate, PollRules.SecondSlotTime);

        return tournaments
            .Where(x => PollRules.IsSupportedTournamentType(x.TypeId))
            .Where(x => x.HasRussianLanguage)
            .Where(x => x.HasChgkGgRating)
            .Where(x => !excludedIds.Contains(x.Id))
            .Select(x => new CandidateAvailability(
                x,
                PollRules.IsAvailableAtSlot(x.DateStart, x.DateEnd, firstSlot),
                PollRules.IsAvailableAtSlot(x.DateStart, x.DateEnd, secondSlot)))
            .Where(x => x.IsAvailableAtFirstSlot || x.IsAvailableAtSecondSlot)
            .OrderByDescending(x => x.Tournament.DifficultyForecast.HasValue)
            .ThenByDescending(x => x.Tournament.DifficultyForecast)
            .ThenBy(x => x.Tournament.Title, StringComparer.Ordinal)
            .ThenBy(x => x.Tournament.Id)
            .Take(PollRules.MaxTournamentOptions)
            .Select(static (x, index) => new PollTournamentCandidate(
                x.Tournament,
                x.IsAvailableAtFirstSlot,
                x.IsAvailableAtSecondSlot,
                index))
            .ToArray();
    }

    private sealed record CandidateAvailability(
        TournamentDetails Tournament,
        bool IsAvailableAtFirstSlot,
        bool IsAvailableAtSecondSlot);
}
