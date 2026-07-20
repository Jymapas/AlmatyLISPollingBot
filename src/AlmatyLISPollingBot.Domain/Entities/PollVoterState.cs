using AlmatyLISPollingBot.Domain.Common;
using AlmatyLISPollingBot.Domain.Enums;

namespace AlmatyLISPollingBot.Domain.Entities;

public sealed class PollVoterState : Entity
{
    public Guid PollSessionId { get; set; }
    public PollVoterKind VoterKind { get; set; }
    public long TelegramPeerId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string OptionPersistentIdsJson { get; set; } = "[]";
    public int LastUpdateId { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
