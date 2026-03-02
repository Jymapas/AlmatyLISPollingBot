using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlmatyLISPollingBot.Infrastructure.Persistence.Repositories;

public sealed class BotSettingsRepository : IBotSettingsRepository
{
    private readonly BotDbContext dbContext;

    public BotSettingsRepository(BotDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<BotSettings?> GetAsync(CancellationToken cancellationToken)
    {
        return dbContext.BotSettings.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task SaveAsync(BotSettings settings, CancellationToken cancellationToken)
    {
        var existing = await dbContext.BotSettings.SingleOrDefaultAsync(cancellationToken);
        if (existing is null)
        {
            await dbContext.BotSettings.AddAsync(settings, cancellationToken);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(settings);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
