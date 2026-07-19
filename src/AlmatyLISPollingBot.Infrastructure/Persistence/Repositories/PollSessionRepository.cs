using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Domain.Entities;
using AlmatyLISPollingBot.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AlmatyLISPollingBot.Infrastructure.Persistence.Repositories;

public sealed class PollSessionRepository : IPollSessionRepository
{
    private readonly BotDbContext dbContext;

    public PollSessionRepository(BotDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<PollSession?> GetActiveAsync(CancellationToken cancellationToken)
    {
        return dbContext.PollSessions
            .Include(x => x.Candidates)
            .Include(x => x.OptionStates)
            .Include(x => x.VoterStates)
            .SingleOrDefaultAsync(x => x.Status == PollLifecycleStatus.Active || x.Status == PollLifecycleStatus.Draft, cancellationToken);
    }

    public Task<PollSession?> GetByTelegramPollIdAsync(string telegramPollId, CancellationToken cancellationToken)
    {
        return dbContext.PollSessions
            .Include(x => x.OptionStates)
            .Include(x => x.VoterStates)
            .SingleOrDefaultAsync(x => x.TelegramPollId == telegramPollId, cancellationToken);
    }

    public Task AddAsync(PollSession pollSession, CancellationToken cancellationToken)
    {
        return dbContext.PollSessions.AddAsync(pollSession, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
