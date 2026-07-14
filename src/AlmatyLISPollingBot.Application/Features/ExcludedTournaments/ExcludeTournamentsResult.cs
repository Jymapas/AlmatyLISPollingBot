namespace AlmatyLISPollingBot.Application.Features.ExcludedTournaments;

public sealed record ExcludeTournamentsResult(
    IReadOnlyCollection<int> AddedTournamentIds,
    IReadOnlyCollection<int> AlreadyExcludedTournamentIds,
    IReadOnlyCollection<string> InvalidTokens,
    bool IsEmptyInput)
{
    public bool IsValid => !IsEmptyInput && InvalidTokens.Count == 0;
}
