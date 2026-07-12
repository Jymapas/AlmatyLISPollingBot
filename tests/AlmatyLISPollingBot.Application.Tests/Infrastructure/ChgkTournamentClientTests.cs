using System.Net;
using System.Text;
using AlmatyLISPollingBot.Infrastructure.Services;
using FluentAssertions;

namespace AlmatyLISPollingBot.Application.Tests.Infrastructure;

public sealed class ChgkTournamentClientTests
{
    [Fact]
    public async Task GetTournamentsIntersectingDateAsync_ShouldMapJsonLdAndFollowNextPage()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler())
        {
            BaseAddress = new Uri("https://api.rating.chgk.info/")
        };
        var sut = new ChgkTournamentClient(httpClient);

        var tournaments = await sut.GetTournamentsIntersectingDateAsync(new DateOnly(2026, 7, 18), CancellationToken.None);

        tournaments.Should().HaveCount(2);
        var firstTournament = tournaments.First();
        firstTournament.TypeId.Should().Be(3);
        firstTournament.HasRussianLanguage.Should().BeTrue();
        firstTournament.HasChgkGgRating.Should().BeTrue();
        firstTournament.Editors.Single().DisplayName.Should().Be("Иванов И.И.");
        firstTournament.QuestionCount.Should().Be(36);
        tournaments.Last().Id.Should().Be(2);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = request.RequestUri!.Query.Contains("page=2", StringComparison.Ordinal)
                ? "{\"member\":[{\"id\":2,\"name\":\"Второй\",\"type\":{\"id\":6},\"dateStart\":\"2026-07-18T08:00:00+05:00\",\"dateEnd\":\"2026-07-18T16:00:00+05:00\",\"languages\":[{\"id\":\"ru\",\"name\":\"Русский\"}],\"ratingSystems\":[\"chgkgg\"],\"editors\":[],\"questionQty\":{\"1\":36},\"paymentCategories\":[]}]}"
                : "{\"member\":[{\"id\":1,\"name\":\"Первый\",\"type\":{\"id\":3},\"dateStart\":\"2026-07-18T08:00:00+05:00\",\"dateEnd\":\"2026-07-18T16:00:00+05:00\",\"difficultyForecast\":5.5,\"languages\":[{\"id\":\"ru\",\"name\":\"Русский\"}],\"ratingSystems\":[\"chgkgg\"],\"editors\":[{\"name\":\"Иван\",\"patronymic\":\"Иванович\",\"surname\":\"Иванов\"}],\"questionQty\":{\"1\":12,\"2\":12,\"3\":12},\"paymentCategories\":[{\"amount\":900,\"currency\":\"RUB\",\"reason\":\"по умолчанию\"}]}],\"view\":{\"next\":\"/tournaments?page=2\"}}";

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/ld+json")
            });
        }
    }
}
