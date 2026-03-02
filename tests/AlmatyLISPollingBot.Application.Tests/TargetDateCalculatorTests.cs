using AlmatyLISPollingBot.Application.Features.Common;
using FluentAssertions;

namespace AlmatyLISPollingBot.Application.Tests;

public sealed class TargetDateCalculatorTests
{
    [Fact]
    public void GetNextSaturday_ShouldReturnUpcomingSaturdayForWeekday()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Almaty");
        var currentInstant = new DateTimeOffset(2026, 3, 2, 5, 0, 0, TimeSpan.Zero);

        var result = TargetDateCalculator.GetNextSaturday(currentInstant, timeZone);

        result.Should().Be(new DateOnly(2026, 3, 7));
    }

    [Fact]
    public void GetNextSaturday_ShouldSkipCurrentDayWhenTodayIsSaturday()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Almaty");
        var currentInstant = new DateTimeOffset(2026, 3, 7, 2, 0, 0, TimeSpan.Zero);

        var result = TargetDateCalculator.GetNextSaturday(currentInstant, timeZone);

        result.Should().Be(new DateOnly(2026, 3, 14));
    }
}
