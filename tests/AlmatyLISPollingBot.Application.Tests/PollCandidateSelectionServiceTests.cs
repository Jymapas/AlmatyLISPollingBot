using AlmatyLISPollingBot.Application.Contracts.Tournaments;
using AlmatyLISPollingBot.Application.Features.Polls.StartPoll;
using AlmatyLISPollingBot.Domain.Common;
using FluentAssertions;

namespace AlmatyLISPollingBot.Application.Tests;

public sealed class PollCandidateSelectionServiceTests
{
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
            CreateTournament(id: 105, title: "No chgkgg", hasChgkGgRating: false, difficultyForecast: 7.5m),
            CreateTournament(id: 106, title: "Not available in slots", dateStart: AtUtcPlusFive(2026, 3, 7, 9, 0), dateEnd: AtUtcPlusFive(2026, 3, 7, 12, 59), difficultyForecast: 5.5m),
            CreateTournament(id: 107, title: "Excluded", difficultyForecast: 5.9m)
        };

        var result = sut.SelectCandidates(
            tournaments,
            TargetSaturday,
            excludedTournamentIds: new[] { 107 });

        result.Select(x => x.Tournament.Id).Should().Equal(101, 102);
        result.Select(x => x.SortOrder).Should().Equal(0, 1);
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

        var result = sut.SelectCandidates(tournaments, TargetSaturday);

        result.Should().HaveCount(PollRules.MaxTournamentOptions);
        result.Select(x => x.Tournament.Id).Should().Equal(12, 11, 10, 9, 8, 7, 6, 5, 4);
        result.Select(x => x.SortOrder).Should().Equal(0, 1, 2, 3, 4, 5, 6, 7, 8);
    }

    [Fact]
    public void SelectCandidates_ShouldMarkAvailabilityForEachExactSlot()
    {
        var tournaments = new[]
        {
            CreateTournament(
                id: 201,
                title: "First slot only",
                dateStart: AtUtcPlusFive(2026, 3, 7, 10, 0),
                dateEnd: AtUtcPlusFive(2026, 3, 7, 14, 0)),
            CreateTournament(
                id: 202,
                title: "Second slot only",
                dateStart: AtUtcPlusFive(2026, 3, 7, 14, 0),
                dateEnd: AtUtcPlusFive(2026, 3, 7, 16, 0))
        };

        var result = sut.SelectCandidates(tournaments, TargetSaturday);

        var firstOnly = result.Single(x => x.Tournament.Id == 201);
        firstOnly.IsAvailableAtFirstSlot.Should().BeTrue();
        firstOnly.IsAvailableAtSecondSlot.Should().BeFalse();

        var secondOnly = result.Single(x => x.Tournament.Id == 202);
        secondOnly.IsAvailableAtFirstSlot.Should().BeFalse();
        secondOnly.IsAvailableAtSecondSlot.Should().BeTrue();
    }

    [Fact]
    public void SelectAllCandidates_ShouldIncludeExcludedTournamentsWithoutLimit()
    {
        var tournaments = Enumerable.Range(1, 12)
            .Select(id => CreateTournament(id, $"Tournament {id:00}", difficultyForecast: id))
            .ToArray();

        var result = sut.SelectAllCandidates(
            tournaments,
            TargetSaturday,
            excludedTournamentIds: new[] { 10 });

        result.Should().HaveCount(12);
        result.Select(x => x.Tournament.Id).Should().Equal(12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1);
        result.Single(x => x.Tournament.Id == 10).IsExcluded.Should().BeTrue();
        result.Where(x => x.Tournament.Id != 10).Should().OnlyContain(x => !x.IsExcluded);
    }

    private static TournamentDetails CreateTournament(
        int id,
        string title,
        int type = 3,
        decimal? difficultyForecast = 5.0m,
        bool hasRussianLanguage = true,
        DateTimeOffset? dateStart = null,
        DateTimeOffset? dateEnd = null,
        bool hasChgkGgRating = true)
    {
        return new TournamentDetails(
            id,
            title,
            type,
            dateStart ?? AtUtcPlusFive(2026, 3, 7, 10, 0),
            dateEnd ?? AtUtcPlusFive(2026, 3, 7, 17, 0),
            difficultyForecast,
            hasRussianLanguage
                ? new[] { new TournamentLanguage("ru", "Русский") }
                : Array.Empty<TournamentLanguage>(),
            hasChgkGgRating ? new[] { "chgkgg" } : Array.Empty<string>(),
            Array.Empty<TournamentEditor>(),
            new Dictionary<int, int>(),
            Array.Empty<TournamentPaymentCategory>());
    }

    private static DateTimeOffset AtUtcPlusFive(int year, int month, int day, int hour, int minute)
    {
        return new DateTimeOffset(year, month, day, hour, minute, 0, PollRules.SlotUtcOffset);
    }
}
