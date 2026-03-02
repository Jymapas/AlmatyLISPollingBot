using AlmatyLISPollingBot.Application.Contracts.Tournaments;

namespace AlmatyLISPollingBot.Application.Abstractions.Tournaments;

public interface IChgkTournamentClient
{
    Task<IReadOnlyCollection<TournamentSummary>> GetTournamentsByDateAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TournamentSummary>> GetTournamentsByIdsAsync(
        IReadOnlyCollection<int> tournamentIds,
        CancellationToken cancellationToken);
}
