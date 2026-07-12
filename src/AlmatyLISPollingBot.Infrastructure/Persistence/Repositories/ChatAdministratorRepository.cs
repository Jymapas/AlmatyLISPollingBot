using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlmatyLISPollingBot.Infrastructure.Persistence.Repositories;

public sealed class ChatAdministratorRepository : IChatAdministratorRepository
{
    private readonly BotDbContext dbContext;

    public ChatAdministratorRepository(BotDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task ReplaceAsync(IReadOnlyCollection<ChatAdministrator> administrators, CancellationToken cancellationToken)
    {
        var existingAdministrators = await dbContext.ChatAdministrators.ToArrayAsync(cancellationToken);
        dbContext.ChatAdministrators.RemoveRange(existingAdministrators);
        await dbContext.ChatAdministrators.AddRangeAsync(administrators, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
