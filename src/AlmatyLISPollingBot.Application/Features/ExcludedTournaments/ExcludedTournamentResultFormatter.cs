using AlmatyLISPollingBot.Application.Contracts.Tournaments;

namespace AlmatyLISPollingBot.Application.Features.ExcludedTournaments;

public static class ExcludedTournamentResultFormatter
{
    public static string Format(
        ExcludeTournamentsResult result,
        IReadOnlyCollection<TournamentDetails> tournaments)
    {
        var titlesById = tournaments.ToDictionary(x => x.Id, x => x.Title);
        var messageParts = new List<string>();
        if (result.AddedTournamentIds.Count > 0)
        {
            messageParts.Add(FormatGroup("Исключены из будущих опросов", result.AddedTournamentIds, titlesById));
        }

        if (result.AlreadyExcludedTournamentIds.Count > 0)
        {
            messageParts.Add(FormatGroup("Уже были исключены", result.AlreadyExcludedTournamentIds, titlesById));
        }

        return string.Join("\n", messageParts);
    }

    private static string FormatGroup(string title, IReadOnlyCollection<int> tournamentIds, IReadOnlyDictionary<int, string> titlesById)
    {
        var tournaments = tournamentIds.Select(tournamentId => string.Concat(
            "• ",
            tournamentId,
            " — ",
            titlesById.TryGetValue(tournamentId, out var tournamentTitle) && !string.IsNullOrWhiteSpace(tournamentTitle)
                ? tournamentTitle
                : "название недоступно"));

        return string.Concat(title, ":\n", string.Join("\n", tournaments));
    }
}
