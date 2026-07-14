using AlmatyLISPollingBot.Application.Contracts.Tournaments;

namespace AlmatyLISPollingBot.Application.Features.Polls.StartPoll;

public sealed record PollTournamentCandidate(
    TournamentDetails Tournament,
    bool IsAvailableAtFirstSlot,
    bool IsAvailableAtSecondSlot,
    int SortOrder);
