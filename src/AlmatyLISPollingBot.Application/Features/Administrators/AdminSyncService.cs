using AlmatyLISPollingBot.Application.Abstractions.Administrators;
using AlmatyLISPollingBot.Application.Abstractions.Clock;
using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Domain.Entities;

namespace AlmatyLISPollingBot.Application.Features.Administrators;

public sealed class AdminSyncService
{
    private readonly IChatAdministratorClient chatAdministratorClient;
    private readonly IChatAdministratorRepository chatAdministratorRepository;
    private readonly IClock clock;

    public AdminSyncService(
        IChatAdministratorClient chatAdministratorClient,
        IChatAdministratorRepository chatAdministratorRepository,
        IClock clock)
    {
        this.chatAdministratorClient = chatAdministratorClient;
        this.chatAdministratorRepository = chatAdministratorRepository;
        this.clock = clock;
    }

    public async Task SynchronizeAsync(long chatId, CancellationToken cancellationToken)
    {
        var administrators = await chatAdministratorClient.GetAdministratorsAsync(chatId, cancellationToken);
        var synchronizedAtUtc = clock.UtcNow;
        var entities = administrators
            .GroupBy(x => x.TelegramUserId)
            .Select(x => x.First())
            .Select(x => new ChatAdministrator
            {
                TelegramUserId = x.TelegramUserId,
                Username = x.Username,
                SyncedAtUtc = synchronizedAtUtc
            })
            .ToArray();

        await chatAdministratorRepository.ReplaceAsync(entities, cancellationToken);
    }
}
