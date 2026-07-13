using AlmatyLISPollingBot.Domain.Common;
using FluentAssertions;

namespace AlmatyLISPollingBot.Domain.Tests;

public sealed class PollRulesTests
{
    [Fact]
    public void GetPollStopAt_ShouldReturnThursdayBeforeTargetSaturdayInAlmatyTime()
    {
        var stopAt = PollRules.GetPollStopAt(new DateOnly(2026, 3, 7), new TimeOnly(21, 0));

        stopAt.Should().Be(new DateTimeOffset(2026, 3, 5, 21, 0, 0, TimeSpan.FromHours(5)));
    }

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

    [Fact]
    public void IsAvailableAtSlot_ShouldIncludeTournamentBoundaries()
    {
        var slot = PollRules.GetSlotStart(new DateOnly(2026, 3, 7), PollRules.FirstSlotTime);

        PollRules.IsAvailableAtSlot(slot, slot, slot).Should().BeTrue();
    }
}
