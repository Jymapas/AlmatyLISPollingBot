using AlmatyLISPollingBot.Application.Abstractions.Clock;
using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Abstractions.Scheduling;
using AlmatyLISPollingBot.Application.Features.Common;
using AlmatyLISPollingBot.Domain.Entities;
using AlmatyLISPollingBot.Domain.Enums;

namespace AlmatyLISPollingBot.Application.Features.Polls.StartPoll;

public sealed class StartPollService
{
    private readonly IClock clock;
    private readonly IBotSettingsRepository settingsRepository;
    private readonly IPollSessionRepository pollSessionRepository;
    private readonly IBackgroundJobScheduler backgroundJobScheduler;

    public StartPollService(
        IClock clock,
        IBotSettingsRepository settingsRepository,
        IPollSessionRepository pollSessionRepository,
        IBackgroundJobScheduler backgroundJobScheduler)
    {
        this.clock = clock;
        this.settingsRepository = settingsRepository;
        this.pollSessionRepository = pollSessionRepository;
        this.backgroundJobScheduler = backgroundJobScheduler;
    }

    public async Task<PollSession> CreateDraftAsync(CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken)
            ?? throw new InvalidOperationException("Bot settings are not initialized.");

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.ApplicationTimeZone);
        var targetDate = TargetDateCalculator.GetNextSaturday(clock.UtcNow, timeZone);
        var activePoll = await pollSessionRepository.GetActiveAsync(cancellationToken);

        if (activePoll is not null)
        {
            activePoll.Status = PollLifecycleStatus.Stopped;
            activePoll.StoppedAtUtc = clock.UtcNow;
        }

        var stopAtLocal = targetDate.ToDateTime(settings.DefaultPollStopTime);
        var stopAtUtc = TimeZoneInfo.ConvertTimeToUtc(stopAtLocal, timeZone);

        var pollSession = new PollSession
        {
            ChatId = settings.TargetChatId,
            TargetDate = targetDate,
            ScheduledStopAtUtc = stopAtUtc,
            StartedAtUtc = clock.UtcNow,
            Status = PollLifecycleStatus.Draft
        };

        await pollSessionRepository.AddAsync(pollSession, cancellationToken);
        await pollSessionRepository.SaveChangesAsync(cancellationToken);
        await backgroundJobScheduler.SchedulePollStopAsync(pollSession.Id, stopAtUtc, cancellationToken);

        return pollSession;
    }
}
