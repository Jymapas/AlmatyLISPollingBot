namespace AlmatyLISPollingBot.Domain.Common;

public static class PollRules
{
    public const int MaxPollOptions = 10;
    public const int ResultOptionSlots = 1;
    public const int MaxTournamentOptions = MaxPollOptions - ResultOptionSlots;
    public const string ResultsOptionTitle = "посмотреть результаты";
}
