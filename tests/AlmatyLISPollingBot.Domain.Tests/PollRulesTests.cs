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
}
