namespace AlmatyLISPollingBot.Application.Features.ExcludedTournaments;

public sealed record UnexcludeTournamentsResult(
    IReadOnlyCollection<int> ReturnedTournamentIds,
    IReadOnlyCollection<int> AlreadyIncludedTournamentIds,
    IReadOnlyCollection<string> InvalidTokens,
    bool IsEmptyInput)
{
    public bool IsValid => !IsEmptyInput && InvalidTokens.Count == 0;
}
