using AlmatyLISPollingBot.Application.Contracts.Tournaments;
using AlmatyLISPollingBot.Domain.Common;

namespace AlmatyLISPollingBot.Application.Features.Polls.StartPoll;

public sealed class PollCandidateSelectionService
{
    public IReadOnlyList<PollTournamentCandidate> SelectCandidates(
        IEnumerable<TournamentSummary> tournaments,
        DateOnly targetDate,
        TimeZoneInfo timeZone,
        IReadOnlyCollection<int>? excludedTournamentIds = null)
    {
        ArgumentNullException.ThrowIfNull(tournaments);
        ArgumentNullException.ThrowIfNull(timeZone);

        var excludedIds = excludedTournamentIds is null
            ? new HashSet<int>()
            : new HashSet<int>(excludedTournamentIds);

        return tournaments
            .Where(x => PollRules.IsSupportedTournamentType(x.Type))
            .Where(x => x.HasRussianLanguage)
            .Where(x => x.GgRating)
            .Where(x => !excludedIds.Contains(x.Id))
            .Where(x => FitsTargetSaturdayWindow(x, targetDate, timeZone))
            .OrderByDescending(x => x.DifficultyForecast ?? decimal.MinValue)
            .ThenBy(x => x.Title, StringComparer.Ordinal)
            .ThenBy(x => x.Id)
            .Take(PollRules.MaxTournamentOptions)
            .Select(static (x, index) => new PollTournamentCandidate(
                x.Id,
                x.Title,
                x.DifficultyForecast,
                index))
            .ToArray();
    }

    private static bool FitsTargetSaturdayWindow(
        TournamentSummary tournament,
        DateOnly targetDate,
        TimeZoneInfo timeZone)
    {
        var localStartDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(tournament.DateStart, timeZone).Date);
        var localEndDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(tournament.DateEnd, timeZone).Date);

        return PollRules.FitsTargetSaturdayWindow(localStartDate, localEndDate, targetDate);
    }
}
