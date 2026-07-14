using AlmatyLISPollingBot.Domain.Entities;

namespace AlmatyLISPollingBot.Application.Abstractions.Persistence;

public interface IChatAdministratorRepository
{
    Task ReplaceAsync(IReadOnlyCollection<ChatAdministrator> administrators, CancellationToken cancellationToken);
}
