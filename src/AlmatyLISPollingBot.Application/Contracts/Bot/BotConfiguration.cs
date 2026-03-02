namespace AlmatyLISPollingBot.Application.Contracts.Bot;

public sealed class BotConfiguration
{
    public const string SectionName = "Bot";

    public string ApplicationTimeZone { get; init; } = "Asia/Almaty";
    public long MainAdminUserId { get; init; }
    public long TargetChatId { get; init; }
    public long TargetChannelId { get; init; }
    public TimeSpan DefaultPollStopTime { get; init; } = TimeSpan.FromHours(21);
    public string DefaultVenue { get; init; } = string.Empty;
}
