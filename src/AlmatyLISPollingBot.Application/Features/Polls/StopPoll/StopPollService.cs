using AlmatyLISPollingBot.Application.Abstractions.Clock;
using AlmatyLISPollingBot.Application.Abstractions.Messaging;
using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Domain.Enums;

namespace AlmatyLISPollingBot.Application.Features.Polls.StopPoll;

public sealed class StopPollService
{
    private readonly IClock clock;
    private readonly IPollSessionRepository pollSessionRepository;
    private readonly IPollPublisher pollPublisher;

    public StopPollService(
        IClock clock,
        IPollSessionRepository pollSessionRepository,
        IPollPublisher pollPublisher)
    {
        this.clock = clock;
        this.pollSessionRepository = pollSessionRepository;
        this.pollPublisher = pollPublisher;
    }

    public async Task<bool> StopActivePollAsync(CancellationToken cancellationToken)
    {
        var activePoll = await pollSessionRepository.GetActiveAsync(cancellationToken);
        if (activePoll is null)
        {
            return false;
        }

        if (activePoll.PollMessageId is not null)
        {
            try
            {
                await pollPublisher.StopPollAsync(activePoll.ChatId, activePoll.PollMessageId.Value, cancellationToken);
            }
            catch (PollNotFoundException)
            {
            }
        }

        activePoll.Status = PollLifecycleStatus.Stopped;
        activePoll.StoppedAtUtc = clock.UtcNow;
        await pollSessionRepository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
