using AlmatyLISPollingBot.Application.Contracts.Bot;
using AlmatyLISPollingBot.Application.Features.Administrators;
using Microsoft.Extensions.Options;

namespace AlmatyLISPollingBot.Worker.HostedServices;

public sealed class BotSettingsInitializationService : IHostedService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IOptions<BotConfiguration> botConfiguration;

    public BotSettingsInitializationService(
        IServiceScopeFactory scopeFactory,
        IOptions<BotConfiguration> botConfiguration)
    {
        this.scopeFactory = scopeFactory;
        this.botConfiguration = botConfiguration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var botSettingsSyncService = scope.ServiceProvider.GetRequiredService<BotSettingsSyncService>();
        await botSettingsSyncService.ExecuteAsync(botConfiguration.Value, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
