using AlmatyLISPollingBot.Application.Abstractions.ExchangeRates;
using AlmatyLISPollingBot.Application.Contracts.ExchangeRates;
using AlmatyLISPollingBot.Application.Contracts.Tournaments;
using AlmatyLISPollingBot.Application.Features.Polls.StartPoll;
using FluentAssertions;

namespace AlmatyLISPollingBot.Application.Tests.Features.Polls.StartPoll;

public sealed class TournamentListFormatterTests
{
    [Fact]
    public async Task FormatAsync_ShouldPreferKztAndEscapeTournamentData()
    {
        var provider = new StubExchangeRateProvider();
        var sut = new TournamentListFormatter(provider);
        var candidate = CreateCandidate(
            title: "A & B",
            paymentCategories: new[]
            {
                new TournamentPaymentCategory(900m, "RUB", "по умолчанию"),
                new TournamentPaymentCategory(5000m, "KZT", "по умолчанию"),
                new TournamentPaymentCategory(3000m, "KZT", "студенты")
            },
            firstSlot: true,
            secondSlot: false);

        var result = await sut.FormatAsync(
            new[] { candidate },
            TournamentIdDisplayMode.WithTournamentId,
            TournamentPaymentCategoriesDisplayMode.All,
            CancellationToken.None);

        result.HasUnconvertedPrices.Should().BeFalse();
        result.Pages.Should().ContainSingle();
        result.Pages[0].Should().Contain("A &amp; B");
        result.Pages[0].Should().Contain("<b>A &amp; B</b>");
        result.Pages[0].Should().Contain("<b>ID:</b> <code>42</code>");
        result.Pages[0].Should().Contain("<b>Стоимость:</b> 5000₸");
        result.Pages[0].Should().Contain("студенты — 3000₸");
        result.Pages[0].Should().Contain("Только первым");
        provider.RequestedCurrencies.Should().BeEmpty();
    }

    [Fact]
    public async Task FormatAsync_ShouldConvertPreferredRubPriceToTenge()
    {
        var provider = new StubExchangeRateProvider
        {
            Quotes = new Dictionary<string, ExchangeRateQuote?>
            {
                ["RUB"] = new ExchangeRateQuote(5.51m, 1, new DateOnly(2026, 7, 13))
            }
        };
        var sut = new TournamentListFormatter(provider);
        var candidate = CreateCandidate(
            paymentCategories: new[] { new TournamentPaymentCategory(900m, "RUB", "по умолчанию") });

        var result = await sut.FormatAsync(
            new[] { candidate },
            TournamentIdDisplayMode.WithTournamentId,
            TournamentPaymentCategoriesDisplayMode.All,
            CancellationToken.None);

        result.HasUnconvertedPrices.Should().BeFalse();
        result.Pages[0].Should().Contain("900₽ (≈4959₸)");
        provider.RequestedCurrencies.Should().Equal("RUB");
    }

    [Fact]
    public async Task FormatAsync_ShouldKeepOriginalPriceWhenRateIsUnavailable()
    {
        var sut = new TournamentListFormatter(new StubExchangeRateProvider());
        var candidate = CreateCandidate(
            paymentCategories: new[] { new TournamentPaymentCategory(10m, "USD", "по умолчанию") });

        var result = await sut.FormatAsync(
            new[] { candidate },
            TournamentIdDisplayMode.WithTournamentId,
            TournamentPaymentCategoriesDisplayMode.All,
            CancellationToken.None);

        result.HasUnconvertedPrices.Should().BeTrue();
        result.Pages[0].Should().Contain("10$");
        result.Pages[0].Should().NotContain("≈");
    }

    [Fact]
    public async Task FormatAsync_ShouldRequestExchangeRatesSequentially()
    {
        var provider = new SequentialRequestDetectingExchangeRateProvider();
        var sut = new TournamentListFormatter(provider);
        var candidates = new[]
        {
            CreateCandidate(paymentCategories: new[] { new TournamentPaymentCategory(900m, "RUB", "по умолчанию") }),
            CreateCandidate(paymentCategories: new[] { new TournamentPaymentCategory(10m, "USD", "по умолчанию") })
        };

        var formattingTask = sut.FormatAsync(
            candidates,
            TournamentIdDisplayMode.WithTournamentId,
            TournamentPaymentCategoriesDisplayMode.All,
            CancellationToken.None);
        await provider.FirstRequestStarted;

        provider.RequestCount.Should().Be(1);
        provider.ReleaseFirstRequest();
        await formattingTask;
    }

    [Theory]
    [InlineData("Турнир (СИНХРОН)  ", "Турнир")]
    [InlineData("Турнир (АСИНХРОН И ОНЛАЙН)  ", "Турнир")]
    [InlineData("Турнир (асинхрон/онлайн)  ", "Турнир")]
    [InlineData("Турнир (АсИнХрОн)  ", "Турнир")]
    public async Task FormatAsync_ShouldRemoveTerminalTechnicalTitleSuffix(string title, string expectedTitle)
    {
        var sut = new TournamentListFormatter(new StubExchangeRateProvider());

        var result = await sut.FormatAsync(
            new[] { CreateCandidate(title: title) },
            TournamentIdDisplayMode.WithTournamentId,
            TournamentPaymentCategoriesDisplayMode.All,
            CancellationToken.None);

        result.Pages.Should().ContainSingle();
        result.Pages[0].Should().Contain($"<b>{expectedTitle}</b>");
        result.Pages[0].Should().NotContain(title.TrimEnd());
    }

    [Fact]
    public async Task FormatAsync_ShouldKeepTechnicalTextOutsideTheEndOfTitle()
    {
        const string title = "Турнир (синхрон) финал";
        var sut = new TournamentListFormatter(new StubExchangeRateProvider());

        var result = await sut.FormatAsync(
            new[] { CreateCandidate(title: title) },
            TournamentIdDisplayMode.WithTournamentId,
            TournamentPaymentCategoriesDisplayMode.All,
            CancellationToken.None);

        result.Pages[0].Should().Contain($"<b>{title}</b>");
    }

    [Fact]
    public async Task FormatAsync_ShouldOmitTournamentIdWhenRequested()
    {
        var sut = new TournamentListFormatter(new StubExchangeRateProvider());

        var result = await sut.FormatAsync(
            new[] { CreateCandidate() },
            TournamentIdDisplayMode.WithoutTournamentId,
            TournamentPaymentCategoriesDisplayMode.All,
            CancellationToken.None);

        result.Pages.Should().ContainSingle();
        result.Pages[0].Should().NotContain("<b>ID:</b>");
        result.Pages[0].Should().Contain("https://rating.chgk.info/tournament/42");
    }

    [Fact]
    public async Task FormatAsync_ShouldShowOnlyPrimaryPaymentCategoryWhenRequested()
    {
        var sut = new TournamentListFormatter(new StubExchangeRateProvider());
        var candidate = CreateCandidate(
            paymentCategories: new[]
            {
                new TournamentPaymentCategory(900m, "RUB", "по умолчанию"),
                new TournamentPaymentCategory(5000m, "KZT", "по умолчанию"),
                new TournamentPaymentCategory(3000m, "KZT", "студенты")
            });

        var result = await sut.FormatAsync(
            new[] { candidate },
            TournamentIdDisplayMode.WithTournamentId,
            TournamentPaymentCategoriesDisplayMode.PrimaryOnly,
            CancellationToken.None);

        result.Pages[0].Should().Contain("<b>Стоимость:</b> 5000₸");
        result.Pages[0].Should().NotContain("900₽");
        result.Pages[0].Should().NotContain("студенты");
        result.Pages[0].Should().NotContain("3000₸");
    }

    [Fact]
    public async Task FormatAsync_ShouldDisplayTournamentDateRangeInRequestedTimeZone()
    {
        var sut = new TournamentListFormatter(new StubExchangeRateProvider());
        var candidate = CreateCandidate();
        var utcTimeZone = TimeZoneInfo.Utc;

        var result = await sut.FormatAsync(
            new[] { candidate },
            TournamentIdDisplayMode.WithTournamentId,
            TournamentPaymentCategoriesDisplayMode.All,
            TournamentDateRangeDisplayMode.WithDateRange,
            utcTimeZone,
            CancellationToken.None);

        result.Pages[0].Should().Contain("<b>Период:</b> 18.07 03:00 — 18.07 13:00");
    }

    private static PollTournamentCandidate CreateCandidate(
        string title = "Турнир",
        IReadOnlyList<TournamentPaymentCategory>? paymentCategories = null,
        bool firstSlot = true,
        bool secondSlot = true)
    {
        var tournament = new TournamentDetails(
            42,
            title,
            3,
            new DateTimeOffset(2026, 7, 18, 8, 0, 0, TimeSpan.FromHours(5)),
            new DateTimeOffset(2026, 7, 18, 18, 0, 0, TimeSpan.FromHours(5)),
            5.8m,
            new[] { new TournamentLanguage("ru", "Русский") },
            new[] { "chgkgg" },
            new[] { new TournamentEditor("Иван", "Иванович", "Иванов") },
            new Dictionary<int, int> { [1] = 12, [2] = 12, [3] = 12 },
            paymentCategories ?? Array.Empty<TournamentPaymentCategory>());

        return new PollTournamentCandidate(tournament, firstSlot, secondSlot, 0);
    }

    private sealed class StubExchangeRateProvider : IExchangeRateProvider
    {
        public IReadOnlyDictionary<string, ExchangeRateQuote?> Quotes { get; init; } = new Dictionary<string, ExchangeRateQuote?>();
        public List<string> RequestedCurrencies { get; } = new();

        public Task<ExchangeRateQuote?> GetKztRateAsync(string currencyCode, CancellationToken cancellationToken)
        {
            RequestedCurrencies.Add(currencyCode);
            Quotes.TryGetValue(currencyCode, out var quote);
            return Task.FromResult(quote);
        }
    }

    private sealed class SequentialRequestDetectingExchangeRateProvider : IExchangeRateProvider
    {
        private readonly TaskCompletionSource firstRequestStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource releaseFirstRequest = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int RequestCount { get; private set; }
        public Task FirstRequestStarted => firstRequestStarted.Task;

        public async Task<ExchangeRateQuote?> GetKztRateAsync(string currencyCode, CancellationToken cancellationToken)
        {
            RequestCount++;
            if (RequestCount == 1)
            {
                firstRequestStarted.SetResult();
                await releaseFirstRequest.Task.WaitAsync(cancellationToken);
            }

            return null;
        }

        public void ReleaseFirstRequest() => releaseFirstRequest.SetResult();
    }
}
