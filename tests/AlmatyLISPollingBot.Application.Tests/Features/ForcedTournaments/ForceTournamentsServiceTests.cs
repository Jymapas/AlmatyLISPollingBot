using AlmatyLISPollingBot.Application.Abstractions.Clock;
using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Abstractions.Tournaments;
using AlmatyLISPollingBot.Application.Contracts.Tournaments;
using AlmatyLISPollingBot.Application.Features.ForcedTournaments;
using AlmatyLISPollingBot.Domain.Entities;
using FluentAssertions;

namespace AlmatyLISPollingBot.Application.Tests.Features.ForcedTournaments;

public sealed class ForceTournamentsServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldVerifyAndQueueOnlyMissingTournamentIds()
    {
        var repository = new StubForcedTournamentRepository(new[] { 12 });
        var sut = new ForceTournamentsService(
            new StubClock(),
            repository,
            new StubTournamentClient(new[] { CreateTournament(12), CreateTournament(34) }));

        var result = await sut.ExecuteAsync(
            "12 https://rating.chgk.info/tournament/34",
            CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.AddedTournamentIds.Should().Equal(34);
        result.AlreadyQueuedTournamentIds.Should().Equal(12);
        result.Tournaments.Select(x => x.Id).Should().BeEquivalentTo(new[] { 12, 34 });
        repository.AddedIds.Should().Equal(34);
        repository.QueuedAtUtc.Should().Be(new DateTimeOffset(2026, 3, 2, 5, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectWholeInputWhenAnyTournamentDoesNotExist()
    {
        var repository = new StubForcedTournamentRepository();
        var sut = new ForceTournamentsService(
            new StubClock(),
            repository,
            new StubTournamentClient(new[] { CreateTournament(12) }));

        var result = await sut.ExecuteAsync("12, 34", CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.NotFoundTournamentIds.Should().Equal(34);
        repository.AddedIds.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectInvalidInputWithoutCallingExternalServices()
    {
        var repository = new StubForcedTournamentRepository();
        var tournamentClient = new StubTournamentClient(Array.Empty<TournamentDetails>());
        var sut = new ForceTournamentsService(new StubClock(), repository, tournamentClient);

        var result = await sut.ExecuteAsync("12, not-a-tournament", CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.InvalidTokens.Should().Equal("not-a-tournament");
        tournamentClient.GetByIdsCallCount.Should().Be(0);
        repository.AddedIds.Should().BeEmpty();
    }

    private static TournamentDetails CreateTournament(int id)
    {
        return new TournamentDetails(
            id,
            $"Tournament {id}",
            3,
            new DateTimeOffset(2026, 3, 7, 10, 0, 0, TimeSpan.FromHours(5)),
            new DateTimeOffset(2026, 3, 7, 17, 0, 0, TimeSpan.FromHours(5)),
            5m,
            new[] { new TournamentLanguage("ru", "Русский") },
            new[] { "chgkgg" },
            Array.Empty<TournamentEditor>(),
            new Dictionary<int, int>(),
            Array.Empty<TournamentPaymentCategory>());
    }

    private sealed class StubClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 3, 2, 5, 0, 0, TimeSpan.Zero);
    }

    private sealed class StubForcedTournamentRepository : IForcedTournamentRepository
    {
        private readonly HashSet<int> existingIds;

        public StubForcedTournamentRepository(IEnumerable<int>? existingIds = null)
        {
            this.existingIds = existingIds is null ? new HashSet<int>() : new HashSet<int>(existingIds);
        }

        public IReadOnlyCollection<int> AddedIds { get; private set; } = Array.Empty<int>();
        public DateTimeOffset? QueuedAtUtc { get; private set; }

        public Task<IReadOnlyCollection<ForcedTournament>> GetQueuedAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<ForcedTournament>>(Array.Empty<ForcedTournament>());

        public Task<IReadOnlyCollection<int>> AddMissingAsync(
            IReadOnlyCollection<int> tournamentIds,
            DateTimeOffset queuedAtUtc,
            CancellationToken cancellationToken)
        {
            AddedIds = tournamentIds.Where(existingIds.Add).ToArray();
            QueuedAtUtc = queuedAtUtc;
            return Task.FromResult(AddedIds);
        }

        public Task RemoveAsync(IReadOnlyCollection<int> tournamentIds, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class StubTournamentClient : IChgkTournamentClient
    {
        private readonly IReadOnlyCollection<TournamentDetails> tournaments;

        public StubTournamentClient(IReadOnlyCollection<TournamentDetails> tournaments)
        {
            this.tournaments = tournaments;
        }

        public int GetByIdsCallCount { get; private set; }

        public Task<IReadOnlyCollection<TournamentDetails>> GetTournamentsIntersectingDateAsync(
            DateOnly targetDate,
            CancellationToken cancellationToken)
            => Task.FromResult(tournaments);

        public Task<IReadOnlyCollection<TournamentDetails>> GetTournamentsByIdsAsync(
            IReadOnlyCollection<int> tournamentIds,
            CancellationToken cancellationToken)
        {
            GetByIdsCallCount++;
            return Task.FromResult<IReadOnlyCollection<TournamentDetails>>(
                tournaments.Where(x => tournamentIds.Contains(x.Id)).ToArray());
        }
    }
}
