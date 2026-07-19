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

    public Task SetExcludedAsync(long telegramUserId, long administratorUserId, DateTimeOffset changedAtUtc, CancellationToken cancellationToken)
    {
        return dbContext.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO shadow_banned_users (""Id"", ""TelegramUserId"", ""IsDeleted"", ""ExcludedAtUtc"", ""ExcludedByTelegramUserId"", ""ReturnedAtUtc"", ""ReturnedByTelegramUserId"", ""Note"")
            VALUES ({Guid.NewGuid()}, {telegramUserId}, FALSE, {changedAtUtc}, {administratorUserId}, NULL, NULL, NULL)
            ON CONFLICT (""TelegramUserId"") DO UPDATE
            SET ""IsDeleted"" = FALSE,
                ""ExcludedAtUtc"" = EXCLUDED.""ExcludedAtUtc"",
                ""ExcludedByTelegramUserId"" = EXCLUDED.""ExcludedByTelegramUserId"",
                ""ReturnedAtUtc"" = NULL,
                ""ReturnedByTelegramUserId"" = NULL
            WHERE shadow_banned_users.""IsDeleted"" = TRUE;", cancellationToken);
    }

    public async Task SetIncludedAsync(long telegramUserId, long administratorUserId, DateTimeOffset changedAtUtc, CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE shadow_banned_users
            SET ""IsDeleted"" = TRUE,
                ""ReturnedAtUtc"" = {changedAtUtc},
                ""ReturnedByTelegramUserId"" = {administratorUserId}
            WHERE ""TelegramUserId"" = {telegramUserId} AND ""IsDeleted"" = FALSE;", cancellationToken);
    }
}
