using AlmatyLISPollingBot.Application.Abstractions.Clock;
using AlmatyLISPollingBot.Application.Abstractions.Messaging;
using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Contracts.Polls;
using AlmatyLISPollingBot.Application.Features.Polls.StopPoll;
using AlmatyLISPollingBot.Domain.Entities;
using AlmatyLISPollingBot.Domain.Enums;
using FluentAssertions;

namespace AlmatyLISPollingBot.Application.Tests.Features.Polls.StopPoll;

public sealed class StopPollServiceTests
{
    [Fact]
    public async Task StopActivePollAsync_ShouldCloseTelegramPollAndPersistStoppedStatus()
    {
        var session = new PollSession
        {
            ChatId = -100123,
            PollMessageId = 44,
            Status = PollLifecycleStatus.Active
        };
        var repository = new StubPollSessionRepository(session);
        var publisher = new StubPollPublisher();
        var sut = new StopPollService(new StubClock(), repository, publisher);

        var stopped = await sut.StopActivePollAsync(CancellationToken.None);

        stopped.Should().BeTrue();
        publisher.StoppedPolls.Should().ContainSingle().Which.Should().Be((-100123L, 44));
        session.Status.Should().Be(PollLifecycleStatus.Stopped);
        session.StoppedAtUtc.Should().Be(new DateTimeOffset(2026, 3, 2, 5, 0, 0, TimeSpan.Zero));
        repository.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task StopActivePollAsync_ShouldPersistStoppedStatusWhenTelegramCannotFindThePoll()
    {
        var session = new PollSession
        {
            ChatId = -100123,
            PollMessageId = 44,
            Status = PollLifecycleStatus.Active
        };
        var repository = new StubPollSessionRepository(session);
        var publisher = new StubPollPublisher { ThrowPollNotFoundOnStop = true };
        var sut = new StopPollService(new StubClock(), repository, publisher);

        var stopped = await sut.StopActivePollAsync(CancellationToken.None);

        stopped.Should().BeTrue();
        session.Status.Should().Be(PollLifecycleStatus.Stopped);
        repository.SaveChangesCalls.Should().Be(1);
    }

    private sealed class StubClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 3, 2, 5, 0, 0, TimeSpan.Zero);
    }

    private sealed class StubPollSessionRepository : IPollSessionRepository
    {
        private readonly PollSession session;

        public StubPollSessionRepository(PollSession session)
        {
            this.session = session;
        }

        public int SaveChangesCalls { get; private set; }

        public Task<PollSession?> GetActiveAsync(CancellationToken cancellationToken) => Task.FromResult<PollSession?>(session);

        public Task AddAsync(PollSession pollSession, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubPollPublisher : IPollPublisher
    {
        public List<(long ChatId, int MessageId)> StoppedPolls { get; } = new();
        public bool ThrowPollNotFoundOnStop { get; set; }

        public Task<int> SendHtmlMessageAsync(long chatId, string message, CancellationToken cancellationToken)
            => Task.FromException<int>(new NotSupportedException());

        public Task<PublishedPoll> SendPollAsync(PollPublicationRequest request, CancellationToken cancellationToken)
            => Task.FromException<PublishedPoll>(new NotSupportedException());

        public Task StopPollAsync(long chatId, int pollMessageId, CancellationToken cancellationToken)
        {
            StoppedPolls.Add((chatId, pollMessageId));
            return ThrowPollNotFoundOnStop
                ? Task.FromException(new PollNotFoundException(new InvalidOperationException()))
                : Task.CompletedTask;
        }

        public Task DeleteMessageAsync(long chatId, int messageId, CancellationToken cancellationToken)
            => Task.FromException(new NotSupportedException());
    }
}
