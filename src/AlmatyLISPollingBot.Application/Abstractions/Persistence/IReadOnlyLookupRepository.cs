namespace AlmatyLISPollingBot.Application.Abstractions.Persistence;

public interface IReadOnlyLookupRepository
{
    Task<IReadOnlyCollection<int>> GetExcludedTournamentIdsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<long>> GetShadowBannedUserIdsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<long>> GetAdminUserIdsAsync(CancellationToken cancellationToken);
}
