using AlmatyLISPollingBot.Application.Contracts.Administrators;

namespace AlmatyLISPollingBot.Application.Abstractions.Administrators;

public interface IChatAdministratorClient
{
    Task<IReadOnlyCollection<ChatAdministratorInfo>> GetAdministratorsAsync(
        long chatId,
        CancellationToken cancellationToken);
}
