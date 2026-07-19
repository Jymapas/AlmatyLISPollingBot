using AlmatyLISPollingBot.Domain.Enums;

namespace AlmatyLISPollingBot.Application.Features.Polls.Results;

public sealed record PollResultsVoter(
    PollVoterKind VoterKind,
    long TelegramPeerId,
    string DisplayName,
    string? Username,
    bool IsExcluded);
