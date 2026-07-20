using AlmatyLISPollingBot.Domain.Entities;

namespace AlmatyLISPollingBot.Application.Features.Polls.StartPoll;

public sealed record PollCandidatePreparationResult(
    BotSettings Settings,
    DateOnly TargetDate,
    DateTimeOffset StopAtUtc,
    IReadOnlyList<PollTournamentCandidate> Candidates,
    IReadOnlyCollection<int> IncludedForcedTournamentIds,
    PollCandidatePreparationRejectionReason? RejectionReason,
    int ForcedCandidateCount);
