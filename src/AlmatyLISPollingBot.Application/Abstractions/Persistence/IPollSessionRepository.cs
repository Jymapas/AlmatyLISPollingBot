using AlmatyLISPollingBot.Domain.Entities;

namespace AlmatyLISPollingBot.Application.Abstractions.Persistence;

public interface IPollSessionRepository
{
    Task<PollSession?> GetActiveAsync(CancellationToken cancellationToken);
    Task AddAsync(PollSession pollSession, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
