namespace AlmatyLISPollingBot.Application.Features.Polls.Results;

public sealed record PollResultsSummary(
    Guid PollSessionId,
    DateTimeOffset? LastSnapshotAtUtc,
    IReadOnlyList<PollResultsOption> Options);
