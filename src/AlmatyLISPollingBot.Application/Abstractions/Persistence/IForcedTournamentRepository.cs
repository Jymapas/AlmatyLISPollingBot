using AlmatyLISPollingBot.Domain.Entities;

namespace AlmatyLISPollingBot.Application.Abstractions.Persistence;

public interface IForcedTournamentRepository
{
    Task<IReadOnlyCollection<ForcedTournament>> GetQueuedAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<int>> AddMissingAsync(
        IReadOnlyCollection<int> tournamentIds,
        DateTimeOffset queuedAtUtc,
        CancellationToken cancellationToken);

    Task RemoveAsync(IReadOnlyCollection<int> tournamentIds, CancellationToken cancellationToken);
}
