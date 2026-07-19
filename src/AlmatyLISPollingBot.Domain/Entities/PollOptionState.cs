using AlmatyLISPollingBot.Domain.Common;

namespace AlmatyLISPollingBot.Domain.Entities;

public sealed class PollOptionState : Entity
{
    public Guid PollSessionId { get; set; }
    public string PersistentId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int Position { get; set; }
    public int TelegramVoterCount { get; set; }
    public bool IsResultsOption { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset LastSnapshotAtUtc { get; set; }
}
