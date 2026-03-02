using AlmatyLISPollingBot.Application.Abstractions.Scheduling;

namespace AlmatyLISPollingBot.Infrastructure.Scheduling;

public sealed class NoOpBackgroundJobScheduler : IBackgroundJobScheduler
{
    public Task ScheduleAdminSyncAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task SchedulePollStopAsync(Guid pollSessionId, DateTimeOffset stopAtUtc, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
