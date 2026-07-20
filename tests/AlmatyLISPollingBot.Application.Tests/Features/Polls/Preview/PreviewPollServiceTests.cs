using AlmatyLISPollingBot.Application.Abstractions.Clock;
using AlmatyLISPollingBot.Application.Abstractions.ExchangeRates;
using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Abstractions.Tournaments;
using AlmatyLISPollingBot.Application.Contracts.ExchangeRates;
using AlmatyLISPollingBot.Application.Contracts.Tournaments;
using AlmatyLISPollingBot.Application.Features.Polls.Preview;
using AlmatyLISPollingBot.Application.Features.Polls.StartPoll;
using AlmatyLISPollingBot.Domain.Entities;
using FluentAssertions;

namespace AlmatyLISPollingBot.Application.Tests.Features.Polls.Preview;

public sealed class PreviewPollServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldPreviewForcedAndRegularCandidatesWithoutMutatingForceQueue()
    {
        var forcedTournament = CreateTournament(
            id: 2,
            title: "Принудительный",
            difficulty: null,
            type: 8,
            hasRussianLanguage: false,
            hasChgkGgRating: false,
            paymentCategories: new[]
            {
                new TournamentPaymentCategory(5000m, "KZT", "по умолчанию"),
                new TournamentPaymentCategory(3000m, "KZT", "студенты")
            });
        var regularTournament = CreateTournament(id: 1, title: "Обычный", difficulty: 9m);
        var excludedTournament = CreateTournament(id: 3, title: "Исключённый", difficulty: 10m);
        var fixture = new PreviewFixture(
            new[] { regularTournament, forcedTournament, excludedTournament },
            forcedTournamentIds: new[] { forcedTournament.Id },
            excludedTournamentIds: new[] { excludedTournament.Id });

        var result = await fixture.CreateService().ExecuteAsync(StartPollRequest.Default, CancellationToken.None);

        result.RejectionReason.Should().BeNull();
        result.Pages.Should().ContainSingle();
        result.Pages[0].IndexOf("<b>Принудительный</b>", StringComparison.Ordinal)
            .Should().BeLessThan(result.Pages[0].IndexOf("<b>Обычный</b>", StringComparison.Ordinal));
        result.Pages[0].Should().NotContain("Исключённый");
        result.Pages[0].Should().NotContain("<b>ID:</b>");
        result.Pages[0].Should().Contain("студенты — 3000₸");
        fixture.ForcedTournamentRepository.RemovedIds.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectPreviewWhenMoreThanNineForcedCandidatesAreAvailable()
    {
        var tournaments = Enumerable.Range(1, 10)
            .Select(id => CreateTournament(
                id,
                $"Принудительный {id}",
                difficulty: null,
                type: 8,
                hasRussianLanguage: false,
                hasChgkGgRating: false))
            .ToArray();
        var fixture = new PreviewFixture(tournaments, forcedTournamentIds: Enumerable.Range(1, 10).ToArray());

        var result = await fixture.CreateService().ExecuteAsync(StartPollRequest.Default, CancellationToken.None);

        result.RejectionReason.Should().Be(PollCandidatePreparationRejectionReason.TooManyForcedCandidates);
        result.ForcedCandidateCount.Should().Be(10);
        result.Pages.Should().BeEmpty();
        fixture.ForcedTournamentRepository.RemovedIds.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectExpiredTargetDateBeforeRequestingTournaments()
    {
        var fixture = new PreviewFixture(new[] { CreateTournament() });

        var result = await fixture.CreateService().ExecuteAsync(
            new StartPollRequest(new DateOnly(2026, 3, 2), 2),
            CancellationToken.None);

        result.RejectionReason.Should().Be(PollCandidatePreparationRejectionReason.TargetDateAlreadyStopped);
        fixture.TournamentClient.RequestedTargetDates.Should().BeEmpty();
    }

    private static TournamentDetails CreateTournament(
        int id = 1,
        string title = "Турнир",
        decimal? difficulty = 5m,
        int type = 3,
        bool hasRussianLanguage = true,
        bool hasChgkGgRating = true,
        IReadOnlyList<TournamentPaymentCategory>? paymentCategories = null)
    {
        var targetDate = new DateOnly(2026, 3, 7);
        return new TournamentDetails(
            id,
            title,
            type,
            new DateTimeOffset(targetDate.ToDateTime(new TimeOnly(12, 0)), TimeSpan.FromHours(5)),
            new DateTimeOffset(targetDate.ToDateTime(new TimeOnly(16, 0)), TimeSpan.FromHours(5)),
            difficulty,
            hasRussianLanguage ? new[] { new TournamentLanguage("ru", "Русский") } : Array.Empty<TournamentLanguage>(),
            hasChgkGgRating ? new[] { "chgkgg" } : Array.Empty<string>(),
            Array.Empty<TournamentEditor>(),
            new Dictionary<int, int> { [1] = 36 },
            paymentCategories ?? Array.Empty<TournamentPaymentCategory>());
    }

    private sealed class PreviewFixture
    {
        private readonly IReadOnlyCollection<TournamentDetails> tournaments;
        private readonly IReadOnlyCollection<int> excludedTournamentIds;

        public PreviewFixture(
            IReadOnlyCollection<TournamentDetails> tournaments,
            IReadOnlyCollection<int>? forcedTournamentIds = null,
            IReadOnlyCollection<int>? excludedTournamentIds = null)
        {
            this.tournaments = tournaments;
            this.excludedTournamentIds = excludedTournamentIds ?? Array.Empty<int>();
            ForcedTournamentRepository = new StubForcedTournamentRepository(forcedTournamentIds);
            TournamentClient = new StubTournamentClient(tournaments);
        }

        public StubForcedTournamentRepository ForcedTournamentRepository { get; }
        public StubTournamentClient TournamentClient { get; }

        public PreviewPollService CreateService()
        {
            var preparationService = new PollCandidatePreparationService(
                new StubClock(),
                new StubSettingsRepository(),
                new StubLookupRepository(excludedTournamentIds),
                ForcedTournamentRepository,
                TournamentClient,
                new PollCandidateSelectionService());
            return new PreviewPollService(
                preparationService,
                new TournamentListFormatter(new StubExchangeRateProvider()));
        }
    }

    private sealed class StubClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 3, 2, 5, 0, 0, TimeSpan.Zero);
    }

    private sealed class StubSettingsRepository : IBotSettingsRepository
    {
        private readonly BotSettings settings = new()
        {
            ApplicationTimeZone = "Asia/Almaty",
            DefaultPollStopTime = new TimeOnly(21, 0)
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

    private sealed class StubForcedTournamentRepository : IForcedTournamentRepository
    {
        private readonly IReadOnlyCollection<ForcedTournament> queuedTournaments;

        public StubForcedTournamentRepository(IReadOnlyCollection<int>? forcedTournamentIds)
        {
            queuedTournaments = (forcedTournamentIds ?? Array.Empty<int>())
                .Select((tournamentId, index) => new ForcedTournament
                {
                    TournamentId = tournamentId,
                    QueuedAtUtc = new DateTimeOffset(2026, 3, 1, 0, index, 0, TimeSpan.Zero)
                })
                .ToArray();
        }

        public IReadOnlyCollection<int> RemovedIds { get; private set; } = Array.Empty<int>();

        public Task<IReadOnlyCollection<ForcedTournament>> GetQueuedAsync(CancellationToken cancellationToken)
            => Task.FromResult(queuedTournaments);

        public Task<IReadOnlyCollection<int>> AddMissingAsync(
            IReadOnlyCollection<int> tournamentIds,
            DateTimeOffset queuedAtUtc,
            CancellationToken cancellationToken)
            => Task.FromResult(tournamentIds);

        public Task RemoveAsync(IReadOnlyCollection<int> tournamentIds, CancellationToken cancellationToken)
        {
            RemovedIds = tournamentIds;
            return Task.CompletedTask;
        }
    }

    private sealed class StubTournamentClient : IChgkTournamentClient
    {
        private readonly IReadOnlyCollection<TournamentDetails> tournaments;

        public StubTournamentClient(IReadOnlyCollection<TournamentDetails> tournaments)
        {
            this.tournaments = tournaments;
        }

        public List<DateOnly> RequestedTargetDates { get; } = new();

        public Task<IReadOnlyCollection<TournamentDetails>> GetTournamentsIntersectingDateAsync(
            DateOnly targetDate,
            CancellationToken cancellationToken)
        {
            RequestedTargetDates.Add(targetDate);
            return Task.FromResult(tournaments);
        }

        public Task<IReadOnlyCollection<TournamentDetails>> GetTournamentsByIdsAsync(
            IReadOnlyCollection<int> tournamentIds,
            CancellationToken cancellationToken)
            => Task.FromResult(tournaments);
    }

    private sealed class StubExchangeRateProvider : IExchangeRateProvider
    {
        public Task<ExchangeRateQuote?> GetKztRateAsync(string currencyCode, CancellationToken cancellationToken)
            => Task.FromResult<ExchangeRateQuote?>(null);
    }
}
