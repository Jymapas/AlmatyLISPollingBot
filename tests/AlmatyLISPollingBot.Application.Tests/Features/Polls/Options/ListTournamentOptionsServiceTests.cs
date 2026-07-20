using AlmatyLISPollingBot.Application.Abstractions.Clock;
using AlmatyLISPollingBot.Application.Abstractions.ExchangeRates;
using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Abstractions.Tournaments;
using AlmatyLISPollingBot.Application.Contracts.ExchangeRates;
using AlmatyLISPollingBot.Application.Contracts.Tournaments;
using AlmatyLISPollingBot.Application.Features.Polls.Options;
using AlmatyLISPollingBot.Application.Features.Polls.StartPoll;
using AlmatyLISPollingBot.Domain.Entities;
using FluentAssertions;

namespace AlmatyLISPollingBot.Application.Tests.Features.Polls.Options;

public sealed class ListTournamentOptionsServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldListAllEligibleTournamentsAndMarkExcludedOnes()
    {
        var tournaments = new[]
        {
            CreateTournament(1, "Первый", 4m),
            CreateTournament(2, "Исключённый", 5m)
        };
        var sut = CreateService(tournaments, excludedTournamentIds: new[] { 2 });

        var result = await sut.ExecuteAsync(CancellationToken.None);

        result.TargetDate.Should().Be(new DateOnly(2026, 3, 7));
        result.Pages.Should().ContainSingle();
        result.Pages[0].Should().Contain("<b>Исключённый</b>");
        result.Pages[0].Should().Contain("🚫 <b>Исключён</b>");
        result.Pages[0].Should().Contain("<b>Первый</b>");
        result.Pages[0].Should().Contain("<b>ID:</b> <code>1</code>");
        result.Pages[0].Should().StartWith("<b>Турниры на 07.03.2026</b>");
        result.Pages[0].Should().Contain("<b>Период:</b> 07.03 12:00 — 07.03 16:00");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNoPagesWhenNoEligibleTournamentsExist()
    {
        var sut = CreateService(Array.Empty<TournamentDetails>(), Array.Empty<int>());

        var result = await sut.ExecuteAsync(CancellationToken.None);

        result.TargetDate.Should().Be(new DateOnly(2026, 3, 7));
        result.Pages.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldShowOnlyPrimaryPaymentCategory()
    {
        var tournament = CreateTournament(
            1,
            "С ценами",
            4m,
            paymentCategories: new[]
            {
                new TournamentPaymentCategory(5000m, "KZT", "по умолчанию"),
                new TournamentPaymentCategory(3000m, "KZT", "студенты")
            });
        var sut = CreateService(new[] { tournament }, Array.Empty<int>());

        var result = await sut.ExecuteAsync(CancellationToken.None);

        result.Pages.Should().ContainSingle();
        result.Pages[0].Should().Contain("<b>Стоимость:</b> 5000₸");
        result.Pages[0].Should().NotContain("студенты");
        result.Pages[0].Should().NotContain("3000₸");
    }

    [Fact]
    public async Task ExecuteExcludedAsync_ShouldListOnlyExcludedEligibleTournaments()
    {
        var tournaments = new[]
        {
            CreateTournament(1, "Обычный", 4m),
            CreateTournament(
                2,
                "Исключённый",
                5m,
                paymentCategories: new[]
                {
                    new TournamentPaymentCategory(5000m, "KZT", "по умолчанию"),
                    new TournamentPaymentCategory(3000m, "KZT", "студенты")
                }),
            CreateTournament(3, "Неподходящий", 6m, type: 8)
        };
        var sut = CreateService(tournaments, excludedTournamentIds: new[] { 2, 3 });

        var result = await sut.ExecuteExcludedAsync(CancellationToken.None);

        result.TargetDate.Should().Be(new DateOnly(2026, 3, 7));
        result.Pages.Should().ContainSingle();
        result.Pages[0].Should().Contain("<b>Исключённый</b>");
        result.Pages[0].Should().Contain("🚫 <b>Исключён</b>");
        result.Pages[0].Should().Contain("<b>ID:</b> <code>2</code>");
        result.Pages[0].Should().Contain("<b>Стоимость:</b> 5000₸");
        result.Pages[0].Should().NotContain("студенты — 3000₸");
        result.Pages[0].Should().StartWith("<b>Турниры на 07.03.2026</b>");
        result.Pages[0].Should().Contain("<b>Период:</b> 07.03 12:00 — 07.03 16:00");
        result.Pages[0].Should().NotContain("Обычный");
        result.Pages[0].Should().NotContain("Неподходящий");
    }

    [Fact]
    public async Task ExecuteExcludedAsync_ShouldReturnNoPagesWhenNoExcludedEligibleTournamentsExist()
    {
        var sut = CreateService(
            new[] { CreateTournament(1, "Обычный", 4m) },
            excludedTournamentIds: new[] { 2 });

        var result = await sut.ExecuteExcludedAsync(CancellationToken.None);

        result.TargetDate.Should().Be(new DateOnly(2026, 3, 7));
        result.Pages.Should().BeEmpty();
    }

    private static ListTournamentOptionsService CreateService(
        IReadOnlyCollection<TournamentDetails> tournaments,
        IReadOnlyCollection<int> excludedTournamentIds)
    {
        return new ListTournamentOptionsService(
            new StubClock(),
            new StubSettingsRepository(),
            new StubLookupRepository(excludedTournamentIds),
            new StubTournamentClient(tournaments),
            new PollCandidateSelectionService(),
            new TournamentListFormatter(new StubExchangeRateProvider()));
    }

    private static TournamentDetails CreateTournament(
        int id,
        string title,
        decimal difficulty,
        int type = 3,
        IReadOnlyList<TournamentPaymentCategory>? paymentCategories = null)
    {
        return new TournamentDetails(
            id,
            title,
            type,
            new DateTimeOffset(2026, 3, 7, 12, 0, 0, TimeSpan.FromHours(5)),
            new DateTimeOffset(2026, 3, 7, 16, 0, 0, TimeSpan.FromHours(5)),
            difficulty,
            new[] { new TournamentLanguage("ru", "Русский") },
            new[] { "chgkgg" },
            Array.Empty<TournamentEditor>(),
            new Dictionary<int, int>(),
            paymentCategories ?? Array.Empty<TournamentPaymentCategory>());
    }

    private sealed class StubClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 3, 2, 5, 0, 0, TimeSpan.Zero);
    }

    private sealed class StubSettingsRepository : IBotSettingsRepository
    {
        private readonly BotSettings settings = new()
        {
            ApplicationTimeZone = "Asia/Almaty"
        };

        public Task<BotSettings?> GetAsync(CancellationToken cancellationToken) => Task.FromResult<BotSettings?>(settings);

        public Task SaveAsync(BotSettings settings, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StubLookupRepository : IReadOnlyLookupRepository
    {
        private readonly IReadOnlyCollection<int> excludedTournamentIds;

        public StubLookupRepository(IReadOnlyCollection<int> excludedTournamentIds)
        {
            this.excludedTournamentIds = excludedTournamentIds;
        }

        public Task<IReadOnlyCollection<int>> GetExcludedTournamentIdsAsync(CancellationToken cancellationToken)
            => Task.FromResult(excludedTournamentIds);

        public Task<IReadOnlyCollection<long>> GetShadowBannedUserIdsAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<long>>(Array.Empty<long>());

        public Task<IReadOnlyCollection<long>> GetAdminUserIdsAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<long>>(Array.Empty<long>());
    }

    private sealed class StubTournamentClient : IChgkTournamentClient
    {
        private readonly IReadOnlyCollection<TournamentDetails> tournaments;

        public StubTournamentClient(IReadOnlyCollection<TournamentDetails> tournaments)
        {
            this.tournaments = tournaments;
        }

        public Task<IReadOnlyCollection<TournamentDetails>> GetTournamentsIntersectingDateAsync(
            DateOnly targetDate,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(tournaments);
        }

        public Task<IReadOnlyCollection<TournamentDetails>> GetTournamentsByIdsAsync(
            IReadOnlyCollection<int> tournamentIds,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(tournaments);
        }
    }

    private sealed class StubExchangeRateProvider : IExchangeRateProvider
    {
        public Task<ExchangeRateQuote?> GetKztRateAsync(string currencyCode, CancellationToken cancellationToken)
            => Task.FromResult<ExchangeRateQuote?>(null);
    }
}
