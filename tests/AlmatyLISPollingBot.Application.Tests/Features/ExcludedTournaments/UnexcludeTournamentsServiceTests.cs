using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Features.ExcludedTournaments;
using FluentAssertions;

namespace AlmatyLISPollingBot.Application.Tests.Features.ExcludedTournaments;

public sealed class UnexcludeTournamentsServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnActiveExcludedTournamentsToPool()
    {
        var repository = new StubExcludedTournamentRepository(new[] { 12, 34 });
        var sut = new UnexcludeTournamentsService(repository);

        var result = await sut.ExecuteAsync("12, https://rating.chgk.info/tournament/34", CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.ReturnedTournamentIds.Should().BeEquivalentTo(new[] { 12, 34 });
        result.AlreadyIncludedTournamentIds.Should().BeEmpty();
        repository.ActiveIds.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectWholeInputWhenAnyTokenIsInvalid()
    {
        var repository = new StubExcludedTournamentRepository(new[] { 12 });
        var sut = new UnexcludeTournamentsService(repository);

        var result = await sut.ExecuteAsync("12, invalid", CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.InvalidTokens.Should().ContainSingle().Which.Should().Be("invalid");
        repository.SoftDeleteWasCalled.Should().BeFalse();
        repository.ActiveIds.Should().ContainSingle().Which.Should().Be(12);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReportTournamentAlreadyInPool()
    {
        var repository = new StubExcludedTournamentRepository(new[] { 12 });
        var sut = new UnexcludeTournamentsService(repository);

        var result = await sut.ExecuteAsync("12, 34", CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.ReturnedTournamentIds.Should().ContainSingle().Which.Should().Be(12);
        result.AlreadyIncludedTournamentIds.Should().ContainSingle().Which.Should().Be(34);
    }

    private sealed class StubExcludedTournamentRepository : IExcludedTournamentRepository
    {
        private readonly HashSet<int> activeIds;

        public StubExcludedTournamentRepository(IEnumerable<int> activeIds)
        {
            this.activeIds = new HashSet<int>(activeIds);
        }

        public IReadOnlyCollection<int> ActiveIds => activeIds;
        public bool SoftDeleteWasCalled { get; private set; }

        public Task<IReadOnlyCollection<int>> AddOrReactivateAsync(
            IReadOnlyCollection<int> tournamentIds,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<int>>(Array.Empty<int>());
        }

        public Task<IReadOnlyCollection<int>> SoftDeleteActiveAsync(
            IReadOnlyCollection<int> tournamentIds,
            CancellationToken cancellationToken)
        {
            SoftDeleteWasCalled = true;
            return Task.FromResult<IReadOnlyCollection<int>>(tournamentIds.Where(activeIds.Remove).ToArray());
        }
    }
}
