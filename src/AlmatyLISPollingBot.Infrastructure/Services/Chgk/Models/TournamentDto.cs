using System.Text.Json.Serialization;
using AlmatyLISPollingBot.Application.Contracts.Tournaments;

namespace AlmatyLISPollingBot.Infrastructure.Services.Chgk.Models;

internal sealed class TournamentDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public TournamentTypeDto? Type { get; init; }
    public DateTimeOffset DateStart { get; init; }
    public DateTimeOffset DateEnd { get; init; }
    public decimal? DifficultyForecast { get; init; }
    [JsonConverter(typeof(TournamentLanguageCollectionJsonConverter))]
    public IReadOnlyList<TournamentLanguageDto> Languages { get; init; } = Array.Empty<TournamentLanguageDto>();
    public IReadOnlyList<string> RatingSystems { get; init; } = Array.Empty<string>();
    public IReadOnlyList<TournamentPersonDto> Editors { get; init; } = Array.Empty<TournamentPersonDto>();
    public IReadOnlyDictionary<string, int> QuestionQty { get; init; } = new Dictionary<string, int>();
    public IReadOnlyList<TournamentPaymentCategoryDto> PaymentCategories { get; init; } = Array.Empty<TournamentPaymentCategoryDto>();

    public TournamentDetails ToSummary()
    {
        var questionQty = QuestionQty
            .Where(x => int.TryParse(x.Key, out _))
            .ToDictionary(x => int.Parse(x.Key), x => x.Value);

        return new TournamentDetails(
            Id,
            Name,
            Type?.Id ?? 0,
            DateStart,
            DateEnd,
            DifficultyForecast,
            Languages.Select(x => new TournamentLanguage(x.Id, x.Name)).ToArray(),
            RatingSystems,
            Editors.Select(x => new TournamentEditor(x.Name, x.Patronymic, x.Surname)).ToArray(),
            questionQty,
            PaymentCategories.Select(x => new TournamentPaymentCategory(x.Amount, x.Currency, x.Reason)).ToArray());
    }
}
