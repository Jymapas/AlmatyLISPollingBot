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
    }

    [Fact]
    public async Task ApplyPollAnswerAsync_ShouldIgnoreUnknownPoll()
    {
        var repository = new InMemoryPollSessionRepository(CreateSession());
        var service = new PollStateUpdateService(repository, new TestClock());

        await service.ApplyPollAnswerAsync(new PollAnswerSnapshot("unknown", PollVoterKind.User, 1, "A", null, Array.Empty<string>(), 1), CancellationToken.None);

        repository.SaveCount.Should().Be(0);
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
        public InMemoryPollSessionRepository(PollSession session) => this.session = session;
        public int SaveCount { get; private set; }
        public Task<PollSession?> GetActiveAsync(CancellationToken cancellationToken) => Task.FromResult<PollSession?>(session);
        public Task<PollSession?> GetByTelegramPollIdAsync(string telegramPollId, CancellationToken cancellationToken) => Task.FromResult(telegramPollId == session.TelegramPollId ? session : null);
        public Task AddAsync(PollSession pollSession, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken) { SaveCount++; return Task.CompletedTask; }
    }
}
