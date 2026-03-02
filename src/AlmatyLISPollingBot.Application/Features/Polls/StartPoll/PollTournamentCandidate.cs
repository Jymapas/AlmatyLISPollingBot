namespace AlmatyLISPollingBot.Application.Features.Polls.StartPoll;

public sealed record PollTournamentCandidate(
    int TournamentId,
    string Title,
    decimal? DifficultyForecast,
    int SortOrder);
