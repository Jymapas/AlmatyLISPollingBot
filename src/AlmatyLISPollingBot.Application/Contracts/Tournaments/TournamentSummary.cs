namespace AlmatyLISPollingBot.Application.Contracts.Tournaments;

public sealed record TournamentSummary(
    int Id,
    string Title,
    int Type,
    bool GgRating,
    DateTimeOffset DateStart,
    DateTimeOffset DateEnd,
    decimal? DifficultyForecast,
    bool HasRussianLanguage);
