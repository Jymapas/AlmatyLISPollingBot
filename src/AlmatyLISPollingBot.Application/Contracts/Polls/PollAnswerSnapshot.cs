using AlmatyLISPollingBot.Domain.Enums;

namespace AlmatyLISPollingBot.Application.Contracts.Polls;

public sealed record PollAnswerSnapshot(
    string TelegramPollId,
    PollVoterKind VoterKind,
    long TelegramPeerId,
    string DisplayName,
    string? Username,
    IReadOnlyList<string> OptionPersistentIds,
    int UpdateId);
