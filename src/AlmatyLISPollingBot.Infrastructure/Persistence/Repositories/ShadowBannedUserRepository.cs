using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlmatyLISPollingBot.Infrastructure.Persistence.Repositories;

public sealed class ShadowBannedUserRepository : IShadowBannedUserRepository
{
    private readonly BotDbContext dbContext;

    public ShadowBannedUserRepository(BotDbContext dbContext) => this.dbContext = dbContext;

    public Task<ShadowBannedUser?> GetAsync(long telegramUserId, CancellationToken cancellationToken) =>
        dbContext.ShadowBannedUsers.SingleOrDefaultAsync(x => x.TelegramUserId == telegramUserId, cancellationToken);

    public async Task SetExcludedAsync(long telegramUserId, long administratorUserId, DateTimeOffset changedAtUtc, CancellationToken cancellationToken)
    {
        var user = await GetAsync(telegramUserId, cancellationToken);
        if (user is null)
        {
            user = new ShadowBannedUser { TelegramUserId = telegramUserId };
            await dbContext.ShadowBannedUsers.AddAsync(user, cancellationToken);
        }

        user.IsDeleted = false;
        user.ExcludedAtUtc = changedAtUtc;
        user.ExcludedByTelegramUserId = administratorUserId;
        user.ReturnedAtUtc = null;
        user.ReturnedByTelegramUserId = null;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetIncludedAsync(long telegramUserId, long administratorUserId, DateTimeOffset changedAtUtc, CancellationToken cancellationToken)
    {
        var user = await GetAsync(telegramUserId, cancellationToken);
        if (user is null)
        {
            return;
        }

        user.IsDeleted = true;
        user.ReturnedAtUtc = changedAtUtc;
        user.ReturnedByTelegramUserId = administratorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
