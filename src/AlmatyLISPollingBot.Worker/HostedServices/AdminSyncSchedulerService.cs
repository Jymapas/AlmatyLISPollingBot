using AlmatyLISPollingBot.Application.Abstractions.Clock;
using AlmatyLISPollingBot.Application.Contracts.Bot;
using AlmatyLISPollingBot.Application.Features.Administrators;
using AlmatyLISPollingBot.Domain.Common;
using Microsoft.Extensions.Options;

namespace AlmatyLISPollingBot.Worker.HostedServices;

public sealed class AdminSyncSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IOptions<BotConfiguration> botConfiguration;
    private readonly IClock clock;
    private readonly ILogger<AdminSyncSchedulerService> logger;

    public AdminSyncSchedulerService(
        IServiceScopeFactory scopeFactory,
        IOptions<BotConfiguration> botConfiguration,
        IClock clock,
        ILogger<AdminSyncSchedulerService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.botConfiguration = botConfiguration;
        this.clock = clock;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await SynchronizeSafelyAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(GetDelayUntilNextRun(clock.UtcNow), stoppingToken);
            await SynchronizeSafelyAsync(stoppingToken);
        }
    }

    private async Task SynchronizeSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var adminSyncService = scope.ServiceProvider.GetRequiredService<AdminSyncService>();
            await adminSyncService.SynchronizeAsync(botConfiguration.Value.TargetChatId, cancellationToken);
            logger.LogInformation("Chat administrator cache synchronized.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed to synchronize chat administrator cache.");
        }
    }

    private static TimeSpan GetDelayUntilNextRun(DateTimeOffset utcNow)
    {
        var localNow = utcNow.ToOffset(PollRules.SlotUtcOffset);
        var nextRun = new DateTimeOffset(
            localNow.Year,
            localNow.Month,
            localNow.Day,
            6,
            0,
            0,
            PollRules.SlotUtcOffset);
        if (nextRun <= localNow)
        {
            nextRun = nextRun.AddDays(1);
        }

        return nextRun - localNow;
    }
}
