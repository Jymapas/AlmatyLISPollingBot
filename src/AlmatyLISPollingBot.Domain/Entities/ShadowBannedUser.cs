using AlmatyLISPollingBot.Domain.Common;

namespace AlmatyLISPollingBot.Domain.Entities;

public sealed class ShadowBannedUser : Entity
{
    public long TelegramUserId { get; set; }
    public string? Note { get; set; }
}
