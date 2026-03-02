using AlmatyLISPollingBot.Domain.Entities;

namespace AlmatyLISPollingBot.Application.Abstractions.Persistence;

public interface IBotSettingsRepository
{
    Task<BotSettings?> GetAsync(CancellationToken cancellationToken);
    Task SaveAsync(BotSettings settings, CancellationToken cancellationToken);
}
