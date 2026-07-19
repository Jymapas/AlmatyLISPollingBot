namespace AlmatyLISPollingBot.Application.Features.Polls.Results;

public sealed record PollResultsOption(
    Guid OptionId,
    string PersistentId,
    string Text,
    int Position,
    int AdjustedCount,
    int RawCount,
    int ExcludedCount,
    int UnmatchedCount);
