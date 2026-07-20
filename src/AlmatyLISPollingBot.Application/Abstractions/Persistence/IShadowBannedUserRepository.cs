using AlmatyLISPollingBot.Domain.Entities;

namespace AlmatyLISPollingBot.Application.Abstractions.Persistence;

public interface IShadowBannedUserRepository
{
    Task<ShadowBannedUser?> GetAsync(long telegramUserId, CancellationToken cancellationToken);
    Task SetExcludedAsync(long telegramUserId, long administratorUserId, DateTimeOffset changedAtUtc, CancellationToken cancellationToken);
    Task SetIncludedAsync(long telegramUserId, long administratorUserId, DateTimeOffset changedAtUtc, CancellationToken cancellationToken);
}
