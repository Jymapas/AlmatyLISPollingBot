using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Contracts.Bot;
using AlmatyLISPollingBot.Domain.Entities;
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
        var settingsRepository = scope.ServiceProvider.GetRequiredService<IBotSettingsRepository>();
        var existingSettings = await settingsRepository.GetAsync(cancellationToken);
        if (existingSettings is not null)
        {
            return;
        }

        var configuration = botConfiguration.Value;
        await settingsRepository.SaveAsync(
            new BotSettings
            {
                TargetChatId = configuration.TargetChatId,
                MainAdminUserId = configuration.MainAdminUserId,
                ApplicationTimeZone = configuration.ApplicationTimeZone,
                DefaultPollStopTime = TimeOnly.FromTimeSpan(configuration.DefaultPollStopTime),
                Venue = configuration.DefaultVenue
            },
            cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
