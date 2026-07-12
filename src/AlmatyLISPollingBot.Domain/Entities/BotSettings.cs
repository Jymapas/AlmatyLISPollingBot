using AlmatyLISPollingBot.Domain.Common;

namespace AlmatyLISPollingBot.Domain.Entities;

public sealed class BotSettings : Entity
{
    public long TargetChatId { get; set; }
    public long MainAdminUserId { get; set; }
    public string ApplicationTimeZone { get; set; } = "Asia/Almaty";
    public TimeOnly DefaultPollStopTime { get; set; } = new(21, 0);
    public string Venue { get; set; } = string.Empty;
}
