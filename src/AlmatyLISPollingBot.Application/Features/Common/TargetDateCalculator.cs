namespace AlmatyLISPollingBot.Application.Features.Common;

public static class TargetDateCalculator
{
    public static DateOnly GetNextSaturday(DateTimeOffset currentInstant, TimeZoneInfo timeZone)
    {
        var localDate = TimeZoneInfo.ConvertTime(currentInstant, timeZone).Date;
        var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)localDate.DayOfWeek + 7) % 7;
        daysUntilSaturday = daysUntilSaturday == 0 ? 7 : daysUntilSaturday;
        return DateOnly.FromDateTime(localDate.AddDays(daysUntilSaturday));
    }
}
