using System.Text.Json;
using AlmatyLISPollingBot.Application.Abstractions.Clock;
using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Contracts.Polls;
using AlmatyLISPollingBot.Domain.Entities;

namespace AlmatyLISPollingBot.Application.Features.Polls.Results;

public sealed class PollStateUpdateService
{
    private static readonly SemaphoreSlim UpdateGate = new(1, 1);

    private readonly IPollSessionRepository pollSessionRepository;
    private readonly IClock clock;

    public PollStateUpdateService(IPollSessionRepository pollSessionRepository, IClock clock)
    {
        this.pollSessionRepository = pollSessionRepository;
        this.clock = clock;
    }

    public Task ApplyPollSnapshotAsync(PollSnapshot snapshot, CancellationToken cancellationToken)
    {
        return ExecuteSerializedAsync(
            () => ApplyPollSnapshotCoreAsync(snapshot, cancellationToken),
            cancellationToken);
    }

    public Task ApplyPollAnswerAsync(PollAnswerSnapshot answer, CancellationToken cancellationToken)
    {
        return ExecuteSerializedAsync(
            () => ApplyPollAnswerCoreAsync(answer, cancellationToken),
            cancellationToken);
    }

    private async Task ApplyPollSnapshotCoreAsync(PollSnapshot snapshot, CancellationToken cancellationToken)
    {
        var session = await pollSessionRepository.GetByTelegramPollIdAsync(snapshot.TelegramPollId, cancellationToken);
        if (session is null)
        {
            return;
        }

        var snapshotIds = snapshot.Options.Select(x => x.PersistentId).ToHashSet(StringComparer.Ordinal);
        foreach (var state in session.OptionStates)
        {
            state.IsActive = snapshotIds.Contains(state.PersistentId);
        }

        foreach (var option in snapshot.Options)
        {
            var state = session.OptionStates.SingleOrDefault(x => x.PersistentId == option.PersistentId);
            if (state is null)
            {
                state = new PollOptionState
                {
                    PollSessionId = session.Id,
                    PersistentId = option.PersistentId,
                    IsResultsOption = false
                };
                session.OptionStates.Add(state);
            }

            state.Text = option.Text;
            state.Position = option.Position;
            state.TelegramVoterCount = option.VoterCount;
            state.IsActive = true;
            state.LastSnapshotAtUtc = clock.UtcNow;
        }

        await pollSessionRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyPollAnswerCoreAsync(PollAnswerSnapshot answer, CancellationToken cancellationToken)
    {
        var session = await pollSessionRepository.GetByTelegramPollIdAsync(answer.TelegramPollId, cancellationToken);
        if (session is null)
        {
            return;
        }

        var voter = session.VoterStates.SingleOrDefault(x =>
            x.VoterKind == answer.VoterKind && x.TelegramPeerId == answer.TelegramPeerId);
        if (voter is not null && voter.LastUpdateId >= answer.UpdateId)
        {
            return;
        }

        if (voter is null)
        {
            voter = new PollVoterState
            {
                PollSessionId = session.Id,
                VoterKind = answer.VoterKind,
                TelegramPeerId = answer.TelegramPeerId
            };
            session.VoterStates.Add(voter);
        }

        voter.DisplayName = answer.DisplayName;
        voter.Username = answer.Username;
        voter.OptionPersistentIdsJson = JsonSerializer.Serialize(answer.OptionPersistentIds.Distinct(StringComparer.Ordinal));
        voter.LastUpdateId = answer.UpdateId;
        voter.UpdatedAtUtc = clock.UtcNow;
        await pollSessionRepository.SaveChangesAsync(cancellationToken);
    }

    private static async Task ExecuteSerializedAsync(Func<Task> operation, CancellationToken cancellationToken)
    {
        await UpdateGate.WaitAsync(cancellationToken);
        try
        {
            await operation();
        }
        finally
        {
            UpdateGate.Release();
        }
    }
}
