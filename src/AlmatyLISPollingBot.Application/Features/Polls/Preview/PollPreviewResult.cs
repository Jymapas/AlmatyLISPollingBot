using AlmatyLISPollingBot.Application.Features.Polls.StartPoll;

namespace AlmatyLISPollingBot.Application.Features.Polls.Preview;

public sealed record PollPreviewResult(
    DateOnly TargetDate,
    int DesiredTournamentCount,
    IReadOnlyList<string> Pages,
    PollCandidatePreparationRejectionReason? RejectionReason,
    int ForcedCandidateCount);
