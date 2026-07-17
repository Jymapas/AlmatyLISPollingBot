using AlmatyLISPollingBot.Application.Contracts.Tournaments;
using AlmatyLISPollingBot.Application.Features.ExcludedTournaments;
using FluentAssertions;

namespace AlmatyLISPollingBot.Application.Tests.Features.ExcludedTournaments;

public sealed class UnexcludeTournamentsResultFormatterTests
{
    [Fact]
    public void Format_ShouldIncludeReturnedAndAlreadyIncludedTournamentIdsAndTitles()
    {
        var result = new UnexcludeTournamentsResult(
            new[] { 13396 },
            new[] { 15 },
            Array.Empty<string>(),
            IsEmptyInput: false);
        var tournaments = new[]
        {
            CreateTournament(13396, "Возвращённый турнир"),
            CreateTournament(15, "Уже включённый турнир")
        };

        var message = UnexcludeTournamentsResultFormatter.Format(result, tournaments);

        message.Should().Be(
            "Возвращены в пул будущих опросов:\n• <a href=\"https://rating.chgk.info/tournament/13396\">13396</a> — Возвращённый турнир\nУже находятся в пуле будущих опросов:\n• <a href=\"https://rating.chgk.info/tournament/15\">15</a> — Уже включённый турнир");
    }

    private static TournamentDetails CreateTournament(int id, string title)
    {
        return new TournamentDetails(
            id,
            title,
            TypeId: 3,
            DateStart: DateTimeOffset.UtcNow,
            DateEnd: DateTimeOffset.UtcNow,
            DifficultyForecast: null,
            Languages: Array.Empty<TournamentLanguage>(),
            RatingSystems: Array.Empty<string>(),
            Editors: Array.Empty<TournamentEditor>(),
            QuestionQty: new Dictionary<int, int>(),
            PaymentCategories: Array.Empty<TournamentPaymentCategory>());
    }
}
