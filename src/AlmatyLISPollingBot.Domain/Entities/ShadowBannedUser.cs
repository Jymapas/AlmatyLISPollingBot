using AlmatyLISPollingBot.Domain.Common;

namespace AlmatyLISPollingBot.Domain.Entities;

public sealed class ShadowBannedUser : Entity
{
    public long TelegramUserId { get; set; }
    public string? Note { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? ExcludedAtUtc { get; set; }
    public long? ExcludedByTelegramUserId { get; set; }
    public DateTimeOffset? ReturnedAtUtc { get; set; }
    public long? ReturnedByTelegramUserId { get; set; }
}
