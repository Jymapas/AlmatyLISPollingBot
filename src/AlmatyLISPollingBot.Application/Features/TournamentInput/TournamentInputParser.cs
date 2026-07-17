using System.Globalization;

namespace AlmatyLISPollingBot.Application.Features.TournamentInput;

public static class TournamentInputParser
{
    private const string TournamentHost = "rating.chgk.info";

    public static TournamentInputParseResult Parse(string? input)
    {
        var tokens = (input ?? string.Empty)
            .Split(new[] { ' ', ',', '\n', '\r', '\t' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length == 0)
        {
            return new TournamentInputParseResult(
                Array.Empty<int>(),
                Array.Empty<string>(),
                IsEmptyInput: true);
        }

        var tournamentIds = new List<int>();
        var seenTournamentIds = new HashSet<int>();
        var invalidTokens = new List<string>();
        foreach (var token in tokens)
        {
            if (TryParseTournamentId(token, out var tournamentId))
            {
                if (seenTournamentIds.Add(tournamentId))
                {
                    tournamentIds.Add(tournamentId);
                }
            }
            else
            {
                invalidTokens.Add(token);
            }
        }

        return new TournamentInputParseResult(tournamentIds, invalidTokens, IsEmptyInput: false);
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
