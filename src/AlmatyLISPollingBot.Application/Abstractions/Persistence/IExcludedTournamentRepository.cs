namespace AlmatyLISPollingBot.Application.Abstractions.Persistence;

public interface IExcludedTournamentRepository
{
    Task<IReadOnlyCollection<int>> AddOrReactivateAsync(
        IReadOnlyCollection<int> tournamentIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<int>> SoftDeleteActiveAsync(
        IReadOnlyCollection<int> tournamentIds,
        CancellationToken cancellationToken);
}
