using AlmatyLISPollingBot.Application.Contracts.Tournaments;
using AlmatyLISPollingBot.Application.Features.Polls.StartPoll;
using AlmatyLISPollingBot.Domain.Common;
using FluentAssertions;

namespace AlmatyLISPollingBot.Application.Tests;

public sealed class PollCandidateSelectionServiceTests
{
    private static readonly TimeZoneInfo AlmatyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Almaty");
    private static readonly DateOnly TargetSaturday = new(2026, 3, 7);

    private readonly PollCandidateSelectionService sut = new();

    [Fact]
    public void SelectCandidates_ShouldApplyAllPollFiltersAndSortDescendingByDifficulty()
    {
        var tournaments = new[]
        {
            CreateTournament(id: 101, title: "Strong", difficultyForecast: 6.1m),
            CreateTournament(id: 102, title: "Medium", difficultyForecast: 4.2m),
            CreateTournament(id: 103, title: "Wrong type", type: 8, difficultyForecast: 8.0m),
            CreateTournament(id: 104, title: "No Russian", hasRussianLanguage: false, difficultyForecast: 7.0m),
            CreateTournament(id: 105, title: "No ggRating", ggRating: false, difficultyForecast: 7.5m),
            CreateTournament(id: 106, title: "Wrong end date", dateEnd: AtLocalAlmaty(2026, 3, 8, 0, 30), difficultyForecast: 5.5m),
            CreateTournament(id: 107, title: "Excluded", difficultyForecast: 5.9m)
        };

        var result = sut.SelectCandidates(
            tournaments,
            TargetSaturday,
            AlmatyTimeZone,
            excludedTournamentIds: new[] { 107 });

        result.Should().BeEquivalentTo(
            new[]
            {
                new PollTournamentCandidate(101, "Strong", 6.1m, 0),
                new PollTournamentCandidate(102, "Medium", 4.2m, 1)
            });
    }

    [Fact]
    public void SelectCandidates_ShouldLimitResultToNineTournaments()
    {
        var tournaments = Enumerable.Range(1, 12)
            .Select(id => CreateTournament(
                id: id,
                title: $"Tournament {id:00}",
                difficultyForecast: id))
            .ToArray();

        var result = sut.SelectCandidates(tournaments, TargetSaturday, AlmatyTimeZone);

        result.Should().HaveCount(PollRules.MaxTournamentOptions);
        result.Select(x => x.TournamentId).Should().Equal(12, 11, 10, 9, 8, 7, 6, 5, 4);
        result.Select(x => x.SortOrder).Should().Equal(0, 1, 2, 3, 4, 5, 6, 7, 8);
    }

    [Fact]
    public void SelectCandidates_ShouldEvaluateDatesInApplicationTimeZone()
    {
        var tournaments = new[]
        {
            CreateTournament(
                id: 201,
                title: "Local Saturday",
                dateStart: AtLocalAlmaty(2026, 3, 7, 10, 0),
                dateEnd: AtLocalAlmaty(2026, 3, 7, 18, 0)),
            CreateTournament(
                id: 202,
                title: "Becomes Sunday in Almaty",
                dateStart: new DateTimeOffset(2026, 3, 7, 23, 30, 0, TimeSpan.Zero),
                dateEnd: new DateTimeOffset(2026, 3, 8, 1, 0, 0, TimeSpan.Zero))
        };

        var result = sut.SelectCandidates(tournaments, TargetSaturday, AlmatyTimeZone);

        result.Select(x => x.TournamentId).Should().Equal(201);
    }

    private static TournamentSummary CreateTournament(
        int id,
        string title,
        int type = 3,
        bool ggRating = true,
        decimal? difficultyForecast = 5.0m,
        bool hasRussianLanguage = true,
        DateTimeOffset? dateStart = null,
        DateTimeOffset? dateEnd = null)
    {
        return new TournamentSummary(
            id,
            title,
            type,
            ggRating,
            dateStart ?? AtLocalAlmaty(2026, 3, 7, 10, 0),
            dateEnd ?? AtLocalAlmaty(2026, 3, 7, 17, 0),
            difficultyForecast,
            hasRussianLanguage);
    }

    private static DateTimeOffset AtLocalAlmaty(int year, int month, int day, int hour, int minute)
    {
        var localDateTime = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);
        var offset = AlmatyTimeZone.GetUtcOffset(localDateTime);
        return new DateTimeOffset(localDateTime, offset);
    }
}
