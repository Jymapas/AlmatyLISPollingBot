using System.Text.Json;
using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Features.Polls.Results;
using AlmatyLISPollingBot.Domain.Entities;
using AlmatyLISPollingBot.Domain.Enums;
using FluentAssertions;

namespace AlmatyLISPollingBot.Application.Tests.Features.Polls.Results;

public sealed class PollResultsServiceTests
{
    [Fact]
    public async Task GetActiveAsync_ShouldExcludeBannedKnownVotesAndExposeUnmatchedTelegramVotes()
    {
        var session = new PollSession { Status = PollLifecycleStatus.Active };
        var option = new PollOptionState { PersistentId = "a", Text = "<A>", Position = 0, TelegramVoterCount = 3, LastSnapshotAtUtc = DateTimeOffset.UnixEpoch };
        session.OptionStates.Add(option);
        session.OptionStates.Add(new PollOptionState { PersistentId = "r", Text = "results", Position = 1, IsResultsOption = true, LastSnapshotAtUtc = DateTimeOffset.UnixEpoch });
        session.VoterStates.Add(new PollVoterState { VoterKind = PollVoterKind.User, TelegramPeerId = 10, DisplayName = "One", OptionPersistentIdsJson = JsonSerializer.Serialize(new[] { "a" }) });
        session.VoterStates.Add(new PollVoterState { VoterKind = PollVoterKind.User, TelegramPeerId = 20, DisplayName = "Two", OptionPersistentIdsJson = JsonSerializer.Serialize(new[] { "a" }) });
        var service = new PollResultsService(new Repository(session), new Lookup(new[] { 10L }));

        var result = await service.GetActiveAsync(CancellationToken.None);

        var item = result!.Options.Should().ContainSingle().Subject;
        item.AdjustedCount.Should().Be(2);
        item.RawCount.Should().Be(3);
        item.ExcludedCount.Should().Be(1);
        item.UnmatchedCount.Should().Be(1);
        PollResultsService.FormatSummary(result, TimeZoneInfo.Utc).Should().Contain("&lt;A&gt;");
    }

    [Fact]
    public async Task GetActiveAsync_ShouldRejectDraftSessionEvenIfRepositoryReturnsIt()
    {
        var session = new PollSession { Status = PollLifecycleStatus.Draft };
        var service = new PollResultsService(new Repository(session), new Lookup(Array.Empty<long>()));

        var result = await service.GetActiveAsync(CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetVotersAsync_ShouldReadStoppedSessionByItsId()
    {
        var session = new PollSession { Status = PollLifecycleStatus.Stopped };
        var option = new PollOptionState { PersistentId = "a", Text = "A", Position = 0 };
        session.OptionStates.Add(option);
        session.VoterStates.Add(new PollVoterState { VoterKind = PollVoterKind.User, TelegramPeerId = 42, DisplayName = "Voter", OptionPersistentIdsJson = JsonSerializer.Serialize(new[] { "a" }) });
        var service = new PollResultsService(new Repository(session), new Lookup(Array.Empty<long>()));

        var voters = await service.GetVotersAsync(session.Id, option.Id, CancellationToken.None);

        voters.Should().ContainSingle().Which.TelegramPeerId.Should().Be(42);
    }

    [Fact]
    public async Task GetActiveAsync_ShouldSortAdjustedCountThenPollPosition()
    {
        var session = new PollSession { Status = PollLifecycleStatus.Active };
        session.OptionStates.Add(new PollOptionState { PersistentId = "late", Text = "Late", Position = 2, TelegramVoterCount = 1 });
        session.OptionStates.Add(new PollOptionState { PersistentId = "early", Text = "Early", Position = 1, TelegramVoterCount = 1 });
        session.OptionStates.Add(new PollOptionState { PersistentId = "top", Text = "Top", Position = 0, TelegramVoterCount = 2 });
        var service = new PollResultsService(new Repository(session), new Lookup(Array.Empty<long>()));

        var result = await service.GetActiveAsync(CancellationToken.None);

        result!.Options.Select(x => x.Text).Should().Equal("Top", "Early", "Late");
    }

    private sealed class Repository : IPollSessionRepository
    {
        private readonly PollSession session;
        public Repository(PollSession session) => this.session = session;
        public Task<PollSession?> GetActiveAsync(CancellationToken cancellationToken) => Task.FromResult<PollSession?>(session);
        public Task<PollSession?> GetByIdAsync(Guid pollSessionId, CancellationToken cancellationToken) => Task.FromResult(pollSessionId == session.Id ? session : null);
        public Task<PollSession?> GetByTelegramPollIdAsync(string telegramPollId, CancellationToken cancellationToken) => Task.FromResult<PollSession?>(null);
        public Task AddAsync(PollSession pollSession, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Lookup : IReadOnlyLookupRepository
    {
        private readonly IReadOnlyCollection<long> excluded;
        public Lookup(IReadOnlyCollection<long> excluded) => this.excluded = excluded;
        public Task<IReadOnlyCollection<int>> GetExcludedTournamentIdsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<int>>(Array.Empty<int>());
        public Task<IReadOnlyCollection<long>> GetShadowBannedUserIdsAsync(CancellationToken cancellationToken) => Task.FromResult(excluded);
        public Task<IReadOnlyCollection<long>> GetAdminUserIdsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<long>>(Array.Empty<long>());
    }
}
