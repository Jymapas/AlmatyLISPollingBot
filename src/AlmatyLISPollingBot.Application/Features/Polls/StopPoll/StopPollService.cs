using AlmatyLISPollingBot.Application.Abstractions.Clock;
using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Domain.Enums;

namespace AlmatyLISPollingBot.Application.Features.Polls.StopPoll;

public sealed class StopPollService
{
    private readonly IClock clock;
    private readonly IPollSessionRepository pollSessionRepository;

    public StopPollService(IClock clock, IPollSessionRepository pollSessionRepository)
    {
        this.clock = clock;
        this.pollSessionRepository = pollSessionRepository;
    }

    public async Task<bool> StopActivePollAsync(CancellationToken cancellationToken)
    {
        var activePoll = await pollSessionRepository.GetActiveAsync(cancellationToken);
        if (activePoll is null)
        {
            return false;
        }

        activePoll.Status = PollLifecycleStatus.Stopped;
        activePoll.StoppedAtUtc = clock.UtcNow;
        await pollSessionRepository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
