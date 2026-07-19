using AlmatyLISPollingBot.Domain.Common;
using AlmatyLISPollingBot.Domain.Enums;

namespace AlmatyLISPollingBot.Domain.Entities;

public sealed class PollSession : Entity
{
    public DateOnly TargetDate { get; set; }
    public int DesiredTournamentCount { get; set; } = PollRules.DefaultDesiredTournamentCount;
    public PollLifecycleStatus Status { get; set; } = PollLifecycleStatus.Draft;
    public long ChatId { get; set; }
    public string? TelegramPollId { get; set; }
    public int? PollMessageId { get; set; }
    public int? ListMessageId { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? ScheduledStopAtUtc { get; set; }
    public DateTimeOffset? StoppedAtUtc { get; set; }
    public List<PollCandidate> Candidates { get; init; } = new();
    public List<PollOptionState> OptionStates { get; init; } = new();
    public List<PollVoterState> VoterStates { get; init; } = new();
}
