namespace AlmatyLISPollingBot.Application.Abstractions.Scheduling;

public interface IBackgroundJobScheduler
{
    Task ScheduleAdminSyncAsync(CancellationToken cancellationToken);
    Task SchedulePollStopAsync(Guid pollSessionId, DateTimeOffset stopAtUtc, CancellationToken cancellationToken);
}
