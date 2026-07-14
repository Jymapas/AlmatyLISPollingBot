using System.Globalization;

namespace AlmatyLISPollingBot.Application.Features.ExcludedTournaments;

public static class ExcludedTournamentInputParser
{
    private const string TournamentHost = "rating.chgk.info";

    public static ExcludeTournamentsResult Parse(string? input)
    {
        var tokens = (input ?? string.Empty)
            .Split(new[] { ' ', ',', '\n', '\r', '\t' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length == 0)
        {
            return new ExcludeTournamentsResult(
                Array.Empty<int>(),
                Array.Empty<int>(),
                Array.Empty<string>(),
                IsEmptyInput: true);
        }

        var tournamentIds = new HashSet<int>();
        var invalidTokens = new List<string>();
        foreach (var token in tokens)
        {
            if (TryParseTournamentId(token, out var tournamentId))
            {
                tournamentIds.Add(tournamentId);
            }
            else
            {
                invalidTokens.Add(token);
            }
        }

        return new ExcludeTournamentsResult(
            tournamentIds.Order().ToArray(),
            Array.Empty<int>(),
            invalidTokens,
            IsEmptyInput: false);
    }

    private static bool TryParseTournamentId(string token, out int tournamentId)
    {
        if (int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out tournamentId)
            && tournamentId > 0)
        {
            return true;
        }

        if (!Uri.TryCreate(token, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || !IsTournamentHost(uri.Host))
        {
            tournamentId = default;
            return false;
        }

        var pathSegments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (pathSegments.Length != 2
            || !string.Equals(pathSegments[0], "tournament", StringComparison.OrdinalIgnoreCase)
            || !int.TryParse(pathSegments[1], NumberStyles.None, CultureInfo.InvariantCulture, out tournamentId)
            || tournamentId <= 0)
        {
            tournamentId = default;
            return false;
        }

        return true;
    }

    private static bool IsTournamentHost(string host)
    {
        return string.Equals(host, TournamentHost, StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, string.Concat("www.", TournamentHost), StringComparison.OrdinalIgnoreCase);
    }
}
