namespace AlmatyLISPollingBot.Application.Contracts.Bot;

public static class BotCommands
{
    public const string Poll = "poll";
    public const string Options = "options";
    public const string Exclude = "exclude";
    public const string Force = "force";
    public const string Cancel = "cancel";
    public const string Stop = "stop";
    public const string MakePost = "makepost";
    public const string UpdateSettings = "update_settings";

    public static string ToMessageCommand(string command) => string.Concat('/', command);
}
