namespace AlmatyLISPollingBot.Application.Contracts.Bot;

public static class BotCommands
{
    public const string Poll = "poll";
    public const string Options = "options";
    public const string Exclude = "exclude";
    public const string Cancel = "cancel";

    public static string ToMessageCommand(string command) => string.Concat('/', command);
}
