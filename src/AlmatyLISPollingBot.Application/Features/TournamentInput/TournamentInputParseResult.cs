namespace AlmatyLISPollingBot.Application.Features.TournamentInput;

public sealed record TournamentInputParseResult(
    IReadOnlyCollection<int> TournamentIds,
    IReadOnlyCollection<string> InvalidTokens,
    bool IsEmptyInput)
{
    public bool IsValid => !IsEmptyInput && InvalidTokens.Count == 0;
}
