using AlmatyLISPollingBot.Domain.Common;

namespace AlmatyLISPollingBot.Domain.Entities;

public sealed class ChatAdministrator : Entity
{
    public long TelegramUserId { get; set; }
    public string? Username { get; set; }
    public DateTimeOffset SyncedAtUtc { get; set; }
}
