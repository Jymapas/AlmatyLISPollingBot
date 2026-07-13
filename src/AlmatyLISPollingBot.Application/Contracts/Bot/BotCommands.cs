namespace AlmatyLISPollingBot.Application.Contracts.Bot;

public static class BotCommands
{
    public const string Poll = "poll";
    public const string Stop = "stop";
    public const string MakePost = "makepost";

    public static string ToMessageCommand(string command) => string.Concat('/', command);
}
