using AlmatyLISPollingBot.Application.Abstractions.Scheduling;

namespace AlmatyLISPollingBot.Worker.HostedServices;

public sealed class AdminSyncSchedulerService : IHostedService
{
    private readonly IBackgroundJobScheduler backgroundJobScheduler;
    private readonly ILogger<AdminSyncSchedulerService> logger;

    public AdminSyncSchedulerService(
        IBackgroundJobScheduler backgroundJobScheduler,
        ILogger<AdminSyncSchedulerService> logger)
    {
        this.backgroundJobScheduler = backgroundJobScheduler;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await backgroundJobScheduler.ScheduleAdminSyncAsync(cancellationToken);
        logger.LogInformation("Admin sync scheduler initialized.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
