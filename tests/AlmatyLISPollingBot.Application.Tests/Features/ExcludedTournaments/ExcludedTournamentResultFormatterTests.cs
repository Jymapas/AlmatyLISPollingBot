using AlmatyLISPollingBot.Application.Contracts.Tournaments;
using AlmatyLISPollingBot.Application.Features.ExcludedTournaments;
using FluentAssertions;

namespace AlmatyLISPollingBot.Application.Tests.Features.ExcludedTournaments;

public sealed class ExcludedTournamentResultFormatterTests
{
    [Fact]
    public void Format_ShouldIncludeTournamentIdsAndTitles()
    {
        var result = new ExcludeTournamentsResult(
            new[] { 13396 },
            new[] { 15 },
            Array.Empty<string>(),
            IsEmptyInput: false);
        var tournaments = new[]
        {
            CreateTournament(13396, "Первый турнир"),
            CreateTournament(15, "Уже исключённый турнир")
        };

        var message = ExcludedTournamentResultFormatter.Format(result, tournaments);

        message.Should().Be(
            "Исключены из будущих опросов:\n• <a href=\"https://rating.chgk.info/tournament/13396\">13396</a> — Первый турнир\nУже были исключены:\n• <a href=\"https://rating.chgk.info/tournament/15\">15</a> — Уже исключённый турнир");
    }

    [Fact]
    public void Format_ShouldIndicateWhenTournamentTitleIsUnavailable()
    {
        var result = new ExcludeTournamentsResult(
            new[] { 13396 },
            Array.Empty<int>(),
            Array.Empty<string>(),
            IsEmptyInput: false);

        var message = ExcludedTournamentResultFormatter.Format(result, Array.Empty<TournamentDetails>());

        message.Should().Be("Исключены из будущих опросов:\n• <a href=\"https://rating.chgk.info/tournament/13396\">13396</a> — название недоступно");
    }

    [Fact]
    public void Format_ShouldEscapeTournamentTitlesForHtml()
    {
        var result = new ExcludeTournamentsResult(
            new[] { 13396 },
            Array.Empty<int>(),
            Array.Empty<string>(),
            IsEmptyInput: false);

        var message = ExcludedTournamentResultFormatter.Format(result, new[] { CreateTournament(13396, "<Турнир & друзья>") });

        message.Should().Contain("&lt;Турнир &amp; друзья&gt;");
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
