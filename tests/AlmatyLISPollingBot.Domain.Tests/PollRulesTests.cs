using AlmatyLISPollingBot.Domain.Common;
using FluentAssertions;

namespace AlmatyLISPollingBot.Domain.Tests;

public sealed class PollRulesTests
{
    [Fact]
    public void MaxTournamentOptions_ShouldReserveOneSlotForResults()
    {
        PollRules.MaxTournamentOptions.Should().Be(9);
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(6, true)]
    [InlineData(8, false)]
    [InlineData(1, false)]
    public void IsSupportedTournamentType_ShouldMatchConfiguredTypes(int tournamentType, bool expected)
    {
        var result = PollRules.IsSupportedTournamentType(tournamentType);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("2026-03-07", "2026-03-07", "2026-03-07", true)]
    [InlineData("2026-03-07", "2026-03-08", "2026-03-07", false)]
    [InlineData("2026-03-06", "2026-03-07", "2026-03-07", false)]
    public void FitsTargetSaturdayWindow_ShouldRequireLocalStartAndEndOnTargetDate(
        string localStartDate,
        string localEndDate,
        string targetDate,
        bool expected)
    {
        var result = PollRules.FitsTargetSaturdayWindow(
            DateOnly.Parse(localStartDate),
            DateOnly.Parse(localEndDate),
            DateOnly.Parse(targetDate));

        result.Should().Be(expected);
    }
}
