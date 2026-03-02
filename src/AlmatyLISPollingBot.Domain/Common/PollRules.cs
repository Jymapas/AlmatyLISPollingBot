namespace AlmatyLISPollingBot.Domain.Common;

public static class PollRules
{
    public const int MaxPollOptions = 10;
    public const int ResultOptionSlots = 1;
    public const int MaxTournamentOptions = MaxPollOptions - ResultOptionSlots;
    public const string ResultsOptionTitle = "посмотреть результаты";

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

    public static bool FitsTargetSaturdayWindow(
        DateOnly localStartDate,
        DateOnly localEndDate,
        DateOnly targetDate)
    {
        return localStartDate == targetDate && localEndDate == targetDate;
    }
}
