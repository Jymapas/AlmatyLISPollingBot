using System.Net;
using AlmatyLISPollingBot.Application.Contracts.Tournaments;

namespace AlmatyLISPollingBot.Application.Features.ExcludedTournaments;

public static class UnexcludeTournamentsResultFormatter
{
    private const int TelegramMessageMaxLength = 4096;
    private const int TournamentTitleMaxLength = 500;

    public static string Format(
        UnexcludeTournamentsResult result,
        IReadOnlyCollection<TournamentDetails> tournaments)
    {
        var titlesById = tournaments.ToDictionary(x => x.Id, x => x.Title);
        var messageParts = new List<string>();
        if (result.ReturnedTournamentIds.Count > 0)
        {
            messageParts.Add(FormatGroup("Возвращены в пул будущих опросов", result.ReturnedTournamentIds, titlesById));
        }

        if (result.AlreadyIncludedTournamentIds.Count > 0)
        {
            messageParts.Add(FormatGroup("Уже находятся в пуле будущих опросов", result.AlreadyIncludedTournamentIds, titlesById));
        }

        return TruncateAtLineBoundary(string.Join("\n", messageParts));
    }

    private static string FormatGroup(string title, IReadOnlyCollection<int> tournamentIds, IReadOnlyDictionary<int, string> titlesById)
    {
        var tournaments = tournamentIds.Select(tournamentId => string.Concat(
            "• ",
            "<a href=\"https://rating.chgk.info/tournament/",
            tournamentId,
            "\">",
            tournamentId,
            "</a>",
            " — ",
            titlesById.TryGetValue(tournamentId, out var tournamentTitle) && !string.IsNullOrWhiteSpace(tournamentTitle)
                ? WebUtility.HtmlEncode(NormalizeTournamentTitle(tournamentTitle))
                : "название недоступно"));

        return string.Concat(title, ":\n", string.Join("\n", tournaments));
    }

    private static string NormalizeTournamentTitle(string title)
    {
        var normalizedTitle = title.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return normalizedTitle.Length <= TournamentTitleMaxLength
            ? normalizedTitle
            : string.Concat(normalizedTitle.AsSpan(0, TournamentTitleMaxLength - 1), "…");
    }

    private static string TruncateAtLineBoundary(string message)
    {
        if (message.Length <= TelegramMessageMaxLength)
        {
            return message;
        }

        var lastLineBreak = message.LastIndexOf('\n', TelegramMessageMaxLength - 1);
        return lastLineBreak > 0
            ? string.Concat(message.AsSpan(0, lastLineBreak), "\n…")
            : string.Concat(message.AsSpan(0, TelegramMessageMaxLength - 1), "…");
    }
}
