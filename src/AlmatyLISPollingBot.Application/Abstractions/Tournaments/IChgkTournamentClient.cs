using AlmatyLISPollingBot.Application.Contracts.Tournaments;

namespace AlmatyLISPollingBot.Application.Abstractions.Tournaments;

public interface IChgkTournamentClient
{
    Task<IReadOnlyCollection<TournamentDetails>> GetTournamentsIntersectingDateAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TournamentDetails>> GetTournamentsByIdsAsync(
        IReadOnlyCollection<int> tournamentIds,
        CancellationToken cancellationToken);
}
