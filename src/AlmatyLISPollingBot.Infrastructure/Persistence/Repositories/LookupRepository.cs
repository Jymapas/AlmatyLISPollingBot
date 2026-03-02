using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AlmatyLISPollingBot.Infrastructure.Persistence.Repositories;

public sealed class LookupRepository : IReadOnlyLookupRepository
{
    private readonly BotDbContext dbContext;

    public LookupRepository(BotDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<int>> GetExcludedTournamentIdsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.ExcludedTournaments.Select(x => x.TournamentId).ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<long>> GetShadowBannedUserIdsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.ShadowBannedUsers.Select(x => x.TelegramUserId).ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<long>> GetAdminUserIdsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.ChatAdministrators.Select(x => x.TelegramUserId).ToArrayAsync(cancellationToken);
    }
}
