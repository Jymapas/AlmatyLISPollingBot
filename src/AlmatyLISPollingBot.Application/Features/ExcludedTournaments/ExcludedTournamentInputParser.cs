using AlmatyLISPollingBot.Application.Features.TournamentInput;

namespace AlmatyLISPollingBot.Application.Features.ExcludedTournaments;

public static class ExcludedTournamentInputParser
{
    public static ExcludeTournamentsResult Parse(string? input)
    {
        var parsedResult = TournamentInputParser.Parse(input);

        return new ExcludeTournamentsResult(
            parsedResult.TournamentIds,
            Array.Empty<int>(),
            parsedResult.InvalidTokens,
            parsedResult.IsEmptyInput);
    }
}
