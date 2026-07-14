namespace AlmatyLISPollingBot.Application.Abstractions.Persistence;

public interface IExcludedTournamentRepository
{
    Task<IReadOnlyCollection<int>> AddMissingAsync(
        IReadOnlyCollection<int> tournamentIds,
        CancellationToken cancellationToken);
}
