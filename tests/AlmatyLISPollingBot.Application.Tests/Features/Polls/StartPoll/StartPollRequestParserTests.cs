using AlmatyLISPollingBot.Application.Features.Polls.StartPoll;
using FluentAssertions;

namespace AlmatyLISPollingBot.Application.Tests.Features.Polls.StartPoll;

public sealed class StartPollRequestParserTests
{
    [Theory]
    [InlineData(null, null, 2)]
    [InlineData("1", null, 1)]
    [InlineData("25.07.2026", "2026-07-25", 2)]
    [InlineData("25.07.2026 1", "2026-07-25", 1)]
    public void Parse_ShouldAcceptSupportedCommandForms(string? input, string? expectedDate, int expectedTournamentCount)
    {
        var result = StartPollRequestParser.Parse(input);

        result.IsValid.Should().BeTrue();
        result.Request!.TargetDate.Should().Be(expectedDate is null ? null : DateOnly.Parse(expectedDate));
        result.Request.DesiredTournamentCount.Should().Be(expectedTournamentCount);
    }

    [Theory]
    [InlineData("2")]
    [InlineData("2026-07-25")]
    [InlineData("25.07.2026 2")]
    [InlineData("1 25.07.2026")]
    [InlineData("25.07.2026 1 extra")]
    [InlineData("31.02.2026")]
    public void Parse_ShouldRejectUnsupportedForms(string input)
    {
        var result = StartPollRequestParser.Parse(input);

        result.IsValid.Should().BeFalse();
    }
}
