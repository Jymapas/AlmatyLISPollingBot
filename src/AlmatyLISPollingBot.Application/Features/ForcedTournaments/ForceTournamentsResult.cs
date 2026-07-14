using AlmatyLISPollingBot.Application.Contracts.Tournaments;

namespace AlmatyLISPollingBot.Application.Features.ForcedTournaments;

public sealed record ForceTournamentsResult(
    IReadOnlyCollection<int> AddedTournamentIds,
    IReadOnlyCollection<int> AlreadyQueuedTournamentIds,
    IReadOnlyCollection<string> InvalidTokens,
    IReadOnlyCollection<int> NotFoundTournamentIds,
    IReadOnlyCollection<TournamentDetails> Tournaments,
    bool IsEmptyInput)
{
    public bool IsValid => !IsEmptyInput
        && InvalidTokens.Count == 0
        && NotFoundTournamentIds.Count == 0;
}
