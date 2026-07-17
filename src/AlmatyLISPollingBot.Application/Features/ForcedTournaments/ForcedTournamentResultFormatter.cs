using System.Net;
using AlmatyLISPollingBot.Application.Contracts.Tournaments;

namespace AlmatyLISPollingBot.Application.Features.ForcedTournaments;

public static class ForcedTournamentResultFormatter
{
    private const int TelegramMessageMaxLength = 4096;
    private const int TournamentTitleMaxLength = 500;

    public static string Format(ForceTournamentsResult result)
    {
        var titlesById = result.Tournaments.ToDictionary(x => x.Id, x => x.Title);
        var messageParts = new List<string>();
        if (result.AddedTournamentIds.Count > 0)
        {
            messageParts.Add(FormatGroup("Добавлены в очередь принудительного включения", result.AddedTournamentIds, titlesById));
        }

        if (result.AlreadyQueuedTournamentIds.Count > 0)
        {
            messageParts.Add(FormatGroup("Уже ожидают принудительного включения", result.AlreadyQueuedTournamentIds, titlesById));
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
