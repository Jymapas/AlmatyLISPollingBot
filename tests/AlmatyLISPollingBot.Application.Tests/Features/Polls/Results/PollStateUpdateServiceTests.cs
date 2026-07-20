using System.Text.Json;
using AlmatyLISPollingBot.Application.Abstractions.Clock;
using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Contracts.Polls;
using AlmatyLISPollingBot.Application.Features.Polls.Results;
using AlmatyLISPollingBot.Domain.Entities;
using AlmatyLISPollingBot.Domain.Enums;
using FluentAssertions;

namespace AlmatyLISPollingBot.Application.Tests.Features.Polls.Results;

public sealed class PollStateUpdateServiceTests
{
    [Fact]
    public async Task ApplyPollSnapshotAsync_ShouldAddDynamicOptionAndDeactivateRemovedOption()
    {
        var session = CreateSession();
        var repository = new InMemoryPollSessionRepository(session);
        var service = new PollStateUpdateService(repository, new TestClock());

        await service.ApplyPollSnapshotAsync(new PollSnapshot("poll", new[]
        {
            new PollOptionSnapshot("a", "A", 0, 4),
            new PollOptionSnapshot("dynamic", "<dynamic>", 2, 1)
        }), CancellationToken.None);

        session.OptionStates.Should().Contain(x => x.PersistentId == "dynamic" && x.IsActive && x.TelegramVoterCount == 1);
        session.OptionStates.Should().Contain(x => x.PersistentId == "results" && !x.IsActive);
        repository.AddedOptionStates.Should().ContainSingle(x => x.PersistentId == "dynamic");
        repository.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task ApplyPollAnswerAsync_ShouldReplaceSelectionAndIgnoreOutOfOrderUpdate()
    {
        var session = CreateSession();
        var repository = new InMemoryPollSessionRepository(session);
        var service = new PollStateUpdateService(repository, new TestClock());
        var current = new PollAnswerSnapshot("poll", PollVoterKind.User, 42, "User", "u", new[] { "a", "results" }, 10);

        await service.ApplyPollAnswerAsync(current, CancellationToken.None);
        await service.ApplyPollAnswerAsync(current with { OptionPersistentIds = new[] { "b" }, UpdateId = 9 }, CancellationToken.None);
        await service.ApplyPollAnswerAsync(current with { OptionPersistentIds = new[] { "b" }, UpdateId = 11 }, CancellationToken.None);

        var voter = session.VoterStates.Should().ContainSingle().Subject;
        (JsonSerializer.Deserialize<string[]>(voter.OptionPersistentIdsJson) ?? Array.Empty<string>()).Should().Equal("b");
        voter.LastUpdateId.Should().Be(11);
        repository.AddedVoterStates.Should().ContainSingle().Which.TelegramPeerId.Should().Be(42);
    }

    [Fact]
    public async Task ApplyPollAnswerAsync_ShouldIgnoreUnknownPoll()
    {
        var repository = new InMemoryPollSessionRepository(CreateSession());
        var service = new PollStateUpdateService(repository, new TestClock());

        await service.ApplyPollAnswerAsync(new PollAnswerSnapshot("unknown", PollVoterKind.User, 1, "A", null, Array.Empty<string>(), 1), CancellationToken.None);

        repository.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task ApplyPollAnswerAsync_ShouldPersistRetractionAsAnEmptyCurrentSelection()
    {
        var session = CreateSession();
        var service = new PollStateUpdateService(new InMemoryPollSessionRepository(session), new TestClock());

        await service.ApplyPollAnswerAsync(new PollAnswerSnapshot("poll", PollVoterKind.User, 1, "A", null, new[] { "a" }, 1), CancellationToken.None);
        await service.ApplyPollAnswerAsync(new PollAnswerSnapshot("poll", PollVoterKind.User, 1, "A", null, Array.Empty<string>(), 2), CancellationToken.None);

        (JsonSerializer.Deserialize<string[]>(session.VoterStates.Single().OptionPersistentIdsJson) ?? Array.Empty<string>()).Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyPollSnapshotAsync_ShouldSerializeUpdatesAcrossServiceInstances()
    {
        var session = CreateSession();
        var saveDetector = new ConcurrentSaveDetector();
        var firstService = new PollStateUpdateService(
            new InMemoryPollSessionRepository(session, saveDetector),
            new TestClock());
        var secondService = new PollStateUpdateService(
            new InMemoryPollSessionRepository(session, saveDetector),
            new TestClock());

        var updates = new[]
        {
            firstService.ApplyPollSnapshotAsync(
                new PollSnapshot("poll", new[] { new PollOptionSnapshot("a", "A", 0, 1) }),
                CancellationToken.None),
            secondService.ApplyPollSnapshotAsync(
                new PollSnapshot("poll", new[] { new PollOptionSnapshot("a", "A", 0, 2) }),
                CancellationToken.None)
        };

        await Task.WhenAll(updates);
    }

    private static PollSession CreateSession()
    {
        var session = new PollSession { TelegramPollId = "poll", Status = PollLifecycleStatus.Active };
        session.OptionStates.AddRange(new[]
        {
            new PollOptionState { PersistentId = "a", Text = "A", Position = 0, LastSnapshotAtUtc = DateTimeOffset.UnixEpoch },
            new PollOptionState { PersistentId = "b", Text = "B", Position = 1, LastSnapshotAtUtc = DateTimeOffset.UnixEpoch },
            new PollOptionState { PersistentId = "results", Text = "Results", Position = 2, IsResultsOption = true, LastSnapshotAtUtc = DateTimeOffset.UnixEpoch }
        });
        return session;
    }

    private sealed class TestClock : IClock { public DateTimeOffset UtcNow => new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero); }

    private sealed class InMemoryPollSessionRepository : IPollSessionRepository
    {
        private readonly PollSession session;
        private readonly ConcurrentSaveDetector? saveDetector;

        public InMemoryPollSessionRepository(PollSession session, ConcurrentSaveDetector? saveDetector = null)
        {
            this.session = session;
            this.saveDetector = saveDetector;
        }
        public int SaveCount { get; private set; }
        public List<PollOptionState> AddedOptionStates { get; } = new();
        public List<PollVoterState> AddedVoterStates { get; } = new();
        public Task<PollSession?> GetActiveAsync(CancellationToken cancellationToken) => Task.FromResult<PollSession?>(session);
        public Task<PollSession?> GetByIdAsync(Guid pollSessionId, CancellationToken cancellationToken) => Task.FromResult(pollSessionId == session.Id ? session : null);
        public Task<PollSession?> GetByTelegramPollIdAsync(string telegramPollId, CancellationToken cancellationToken) => Task.FromResult(telegramPollId == session.TelegramPollId ? session : null);
        public Task AddAsync(PollSession pollSession, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AddOptionStateAsync(PollOptionState optionState, CancellationToken cancellationToken)
        {
            AddedOptionStates.Add(optionState);
            return Task.CompletedTask;
        }
        public Task AddVoterStateAsync(PollVoterState voterState, CancellationToken cancellationToken)
        {
            AddedVoterStates.Add(voterState);
            return Task.CompletedTask;
        }
        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            if (saveDetector is not null)
            {
                await saveDetector.SaveAsync(cancellationToken);
            }
        }
    }

    private sealed class ConcurrentSaveDetector
    {
        private int activeSaveCount;

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref activeSaveCount) != 1)
            {
                Interlocked.Decrement(ref activeSaveCount);
                throw new InvalidOperationException("Poll state saves must not run concurrently.");
            }

            try
            {
                await Task.Delay(10, cancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref activeSaveCount);
            }
        }
    }
}
