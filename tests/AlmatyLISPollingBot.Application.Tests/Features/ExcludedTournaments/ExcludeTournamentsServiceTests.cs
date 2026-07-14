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
        private readonly HashSet<int> existingIds;

        public StubExcludedTournamentRepository(IEnumerable<int>? existingIds = null)
        {
            this.existingIds = existingIds is null ? new HashSet<int>() : new HashSet<int>(existingIds);
        }

        public IReadOnlyCollection<int> AddedIds { get; private set; } = Array.Empty<int>();

        public Task<IReadOnlyCollection<int>> AddMissingAsync(
            IReadOnlyCollection<int> tournamentIds,
            CancellationToken cancellationToken)
        {
            AddedIds = tournamentIds.Where(existingIds.Add).ToArray();
            return Task.FromResult(AddedIds);
        }
    }
}
