namespace AlmatyLISPollingBot.Application.Contracts.Tournaments;

public sealed record TournamentDetails(
    int Id,
    string Title,
    int TypeId,
    DateTimeOffset DateStart,
    DateTimeOffset DateEnd,
    decimal? DifficultyForecast,
    IReadOnlyList<TournamentLanguage> Languages,
    IReadOnlyList<string> RatingSystems,
    IReadOnlyList<TournamentEditor> Editors,
    IReadOnlyDictionary<int, int> QuestionQty,
    IReadOnlyList<TournamentPaymentCategory> PaymentCategories)
{
    public bool HasRussianLanguage => Languages.Any(x => string.Equals(x.Id, "ru", StringComparison.OrdinalIgnoreCase));

    public bool HasChgkGgRating => RatingSystems.Any(x => string.Equals(x, "chgkgg", StringComparison.OrdinalIgnoreCase));

    public int QuestionCount => QuestionQty.Values.Sum();
}
