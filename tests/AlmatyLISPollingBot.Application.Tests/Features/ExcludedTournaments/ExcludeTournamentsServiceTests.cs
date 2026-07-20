using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Features.ExcludedTournaments;
using FluentAssertions;

namespace AlmatyLISPollingBot.Application.Tests.Features.ExcludedTournaments;

public sealed class ExcludeTournamentsServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldAddOnlyNewTournamentIds()
    {
        var repository = new StubExcludedTournamentRepository(new[] { 12 });
        var sut = new ExcludeTournamentsService(repository);

        var result = await sut.ExecuteAsync("12, 34 56", CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.AddedTournamentIds.Should().BeEquivalentTo(new[] { 34, 56 });
        result.AlreadyExcludedTournamentIds.Should().BeEquivalentTo(new[] { 12 });
        repository.AddedIds.Should().BeEquivalentTo(new[] { 34, 56 });
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectWholeInputWhenAnyTokenIsInvalid()
    {
        var repository = new StubExcludedTournamentRepository();
        var sut = new ExcludeTournamentsService(repository);

        var result = await sut.ExecuteAsync("12, invalid, 34", CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.InvalidTokens.Should().ContainSingle().Which.Should().Be("invalid");
        repository.AddedIds.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReactivateSoftDeletedTournament()
    {
        var repository = new StubExcludedTournamentRepository(softDeletedIds: new[] { 12 });
        var sut = new ExcludeTournamentsService(repository);

        var result = await sut.ExecuteAsync("12", CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.AddedTournamentIds.Should().ContainSingle().Which.Should().Be(12);
        repository.ReactivatedIds.Should().ContainSingle().Which.Should().Be(12);
        repository.ActiveIds.Should().Contain(12);
    }

    [Fact]
    public void Parse_ShouldAcceptIdsSeparatedByWhitespaceAndCommas()
    {
        var result = ExcludedTournamentInputParser.Parse("12, 34\n56");

        result.IsValid.Should().BeTrue();
        result.AddedTournamentIds.Should().BeEquivalentTo(new[] { 12, 34, 56 });
    }

    [Fact]
    public void Parse_ShouldAcceptTournamentLinks()
    {
        var result = ExcludedTournamentInputParser.Parse(
            "https://rating.chgk.info/tournament/12 https://www.rating.chgk.info/tournament/34/?ref=bot");

        result.IsValid.Should().BeTrue();
        result.AddedTournamentIds.Should().BeEquivalentTo(new[] { 12, 34 });
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("https://example.com/tournament/12")]
    [InlineData("https://rating.chgk.info/tournaments/12")]
    public void Parse_ShouldRejectEmptyAndInvalidInput(string input)
    {
        var result = ExcludedTournamentInputParser.Parse(input);

        result.IsValid.Should().BeFalse();
    }

    private sealed class StubExcludedTournamentRepository : IExcludedTournamentRepository
    {
        private readonly HashSet<int> activeIds;
        private readonly HashSet<int> softDeletedIds;

        public StubExcludedTournamentRepository(
            IEnumerable<int>? activeIds = null,
            IEnumerable<int>? softDeletedIds = null)
        {
            this.activeIds = activeIds is null ? new HashSet<int>() : new HashSet<int>(activeIds);
            this.softDeletedIds = softDeletedIds is null ? new HashSet<int>() : new HashSet<int>(softDeletedIds);
        }

        public IReadOnlyCollection<int> AddedIds { get; private set; } = Array.Empty<int>();
        public IReadOnlyCollection<int> ReactivatedIds { get; private set; } = Array.Empty<int>();
        public IReadOnlyCollection<int> ActiveIds => activeIds;

        public Task<IReadOnlyCollection<int>> AddOrReactivateAsync(
            IReadOnlyCollection<int> tournamentIds,
            CancellationToken cancellationToken)
        {
            var addedTournamentIds = new List<int>();
            var reactivatedTournamentIds = new List<int>();
            foreach (var tournamentId in tournamentIds)
            {
                if (activeIds.Contains(tournamentId))
                {
                    continue;
                }

                if (softDeletedIds.Remove(tournamentId))
                {
                    reactivatedTournamentIds.Add(tournamentId);
                }
                else
                {
                    addedTournamentIds.Add(tournamentId);
                }

                activeIds.Add(tournamentId);
            }

            AddedIds = addedTournamentIds.Concat(reactivatedTournamentIds).ToArray();
            ReactivatedIds = reactivatedTournamentIds;
            return Task.FromResult(AddedIds);
        }

        public Task<IReadOnlyCollection<int>> SoftDeleteActiveAsync(
            IReadOnlyCollection<int> tournamentIds,
            CancellationToken cancellationToken)
        {
            var softDeletedTournamentIds = tournamentIds.Where(activeIds.Remove).ToArray();
            foreach (var tournamentId in softDeletedTournamentIds)
            {
                softDeletedIds.Add(tournamentId);
            }

            return Task.FromResult<IReadOnlyCollection<int>>(softDeletedTournamentIds);
        }
    }
}
