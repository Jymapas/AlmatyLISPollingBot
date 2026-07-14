namespace AlmatyLISPollingBot.Application.Contracts.Bot;

public static class BotCommands
{
    public const string Poll = "poll";

    public static string ToMessageCommand(string command) => string.Concat('/', command);
}
