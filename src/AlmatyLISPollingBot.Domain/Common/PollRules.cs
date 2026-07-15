namespace AlmatyLISPollingBot.Domain.Common;

public static class PollRules
{
    public const int MaxPollOptions = 10;
    public const int ResultOptionSlots = 1;
    public const int MaxTournamentOptions = MaxPollOptions - ResultOptionSlots;
    public const int PollStopDaysBeforeTargetDate = 1;
    public const string ResultsOptionTitle = "посмотреть результаты";
    public static readonly TimeSpan SlotUtcOffset = TimeSpan.FromHours(5);
    public static readonly TimeOnly FirstSlotTime = new(13, 0);
    public static readonly TimeOnly SecondSlotTime = new(15, 30);

    private static readonly HashSet<int> SupportedTournamentTypesInternal = new()
    {
        3,
        6
    };

    public static IReadOnlyCollection<int> SupportedTournamentTypes => SupportedTournamentTypesInternal;

    public static bool IsSupportedTournamentType(int tournamentType)
    {
        return SupportedTournamentTypesInternal.Contains(tournamentType);
    }

    public static DateTimeOffset GetSlotStart(DateOnly targetDate, TimeOnly slotTime)
    {
        return new DateTimeOffset(targetDate.ToDateTime(slotTime), SlotUtcOffset);
    }

    public static DateTimeOffset GetPollStopAt(DateOnly targetDate, TimeOnly stopTime)
    {
        var stopDate = targetDate.AddDays(-PollStopDaysBeforeTargetDate);
        return new DateTimeOffset(stopDate.ToDateTime(stopTime), SlotUtcOffset);
    }

    public static bool IsAvailableAtSlot(
        DateTimeOffset tournamentStart,
        DateTimeOffset tournamentEnd,
        DateTimeOffset slotStart)
    {
        return tournamentStart <= slotStart && slotStart <= tournamentEnd;
    }
}
