using AlmatyLISPollingBot.Application.Abstractions.Clock;
using AlmatyLISPollingBot.Application.Abstractions.ExchangeRates;
using AlmatyLISPollingBot.Application.Abstractions.Messaging;
using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Abstractions.Tournaments;
using AlmatyLISPollingBot.Application.Contracts.ExchangeRates;
using AlmatyLISPollingBot.Application.Contracts.Polls;
using AlmatyLISPollingBot.Application.Contracts.Tournaments;
using AlmatyLISPollingBot.Application.Features.Polls.StartPoll;
using AlmatyLISPollingBot.Domain.Entities;
using FluentAssertions;

namespace AlmatyLISPollingBot.Application.Tests.Features.Polls.StartPoll;

public sealed class StartPollServiceTests
{
    [Fact]
    public async Task StartAsync_ShouldPublishSingleCandidatePollWithAddingOptionsEnabled()
    {
        var fixture = new PollFixture(new[]
        {
            CreateTournament(paymentCategories: new[]
            {
                new TournamentPaymentCategory(5000m, "KZT", "по умолчанию"),
                new TournamentPaymentCategory(3000m, "KZT", "студенты")
            })
        });

        var session = await fixture.CreateService().StartAsync(CancellationToken.None);

        session.Should().NotBeNull();
        session!.Status.Should().Be(AlmatyLISPollingBot.Domain.Enums.PollLifecycleStatus.Active);
        session.ListMessageId.Should().Be(101);
        session.PollMessageId.Should().Be(102);
        session.TelegramPollId.Should().Be("telegram-poll-id");
        session.ScheduledStopAtUtc.Should().Be(new DateTimeOffset(2026, 3, 6, 16, 0, 0, TimeSpan.Zero));
        session.DesiredTournamentCount.Should().Be(2);
        session.Candidates.Should().ContainSingle();
        session.Candidates[0].IsAvailableAtFirstSlot.Should().BeTrue();
        session.Candidates[0].IsAvailableAtSecondSlot.Should().BeTrue();

        fixture.PollPublisher.HtmlMessages.Should().ContainSingle();
        fixture.PollPublisher.HtmlMessages[0].Should().NotContain("<b>ID:</b>");
        fixture.PollPublisher.HtmlMessages[0].Should().Contain("студенты — 3000₸");
        fixture.PollPublisher.PollRequests.Should().ContainSingle();
        var request = fixture.PollPublisher.PollRequests[0];
        request.Question.Should().Be("Выбираем 2 синхрона на субботу, 07.03.2026:");
        request.Options.Should().Equal("Синхрон", "посмотреть результаты");
        request.IsAnonymous.Should().BeFalse();
        request.AllowsMultipleAnswers.Should().BeTrue();
        request.ShuffleOptions.Should().BeFalse();
        request.AllowAddingOptions.Should().BeTrue();
        request.CloseDateUtc.Should().Be(new DateTimeOffset(2026, 3, 6, 16, 0, 0, TimeSpan.Zero));
        fixture.ChatBotClient.Alerts.Should().BeEmpty();
    }

    [Fact]
    public async Task StartAsync_ShouldUseExplicitTargetDateForPollAndStopTime()
    {
        var targetDate = new DateOnly(2026, 3, 10);
        var fixture = new PollFixture(new[] { CreateTournament(targetDate: targetDate) });

        var result = await fixture.CreateService().StartAsync(
            new StartPollRequest(targetDate, 2),
            CancellationToken.None);

        result.RejectionReason.Should().BeNull();
        result.PollSession.Should().NotBeNull();
        result.PollSession!.TargetDate.Should().Be(targetDate);
        result.PollSession.DesiredTournamentCount.Should().Be(2);
        result.PollSession.ScheduledStopAtUtc.Should().Be(new DateTimeOffset(2026, 3, 9, 16, 0, 0, TimeSpan.Zero));
        fixture.TournamentClient.RequestedTargetDates.Should().ContainSingle().Which.Should().Be(targetDate);
        fixture.PollPublisher.PollRequests.Should().ContainSingle().Which.Question
            .Should().Be("Выбираем 2 синхрона на 10.03.2026:");
    }

    [Fact]
    public async Task StartAsync_ShouldCreateSingleChoicePollWhenOneTournamentIsRequested()
    {
        var fixture = new PollFixture(new[] { CreateTournament() });

        var result = await fixture.CreateService().StartAsync(
            new StartPollRequest(null, 1),
            CancellationToken.None);

        result.RejectionReason.Should().BeNull();
        result.PollSession!.DesiredTournamentCount.Should().Be(1);
        var request = fixture.PollPublisher.PollRequests.Should().ContainSingle().Which;
        request.Question.Should().Be("Выбираем 1 синхрон на субботу, 07.03.2026:");
        request.AllowsMultipleAnswers.Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_ShouldRejectDateWhenAutomaticStopTimeHasPassed()
    {
        var fixture = new PollFixture(new[] { CreateTournament() });

        var result = await fixture.CreateService().StartAsync(
            new StartPollRequest(new DateOnly(2026, 3, 2), 2),
            CancellationToken.None);

        result.PollSession.Should().BeNull();
        result.RejectionReason.Should().Be(PollStartRejectionReason.TargetDateAlreadyStopped);
        fixture.TournamentClient.RequestedTargetDates.Should().BeEmpty();
        fixture.PollPublisher.PollRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task StartAsync_ShouldAlertAndSkipPublicationWhenNoCandidatesExist()
    {
        var fixture = new PollFixture(Array.Empty<TournamentDetails>());

        var session = await fixture.CreateService().StartAsync(CancellationToken.None);

        session.Should().BeNull();
        fixture.PollPublisher.HtmlMessages.Should().BeEmpty();
        fixture.PollPublisher.PollRequests.Should().BeEmpty();
        fixture.ChatBotClient.Alerts.Should().ContainSingle().Which.Should().Contain("Не найдено подходящих");
    }

    [Fact]
    public async Task StartAsync_ShouldPublishNonAnonymousPollWithoutAddingOptionsWhenNineCandidatesArePublished()
    {
        var tournaments = Enumerable.Range(1, 9)
            .Select(id => CreateTournament(id, $"Синхрон {id}", id))
            .ToArray();
        var fixture = new PollFixture(tournaments);

        await fixture.CreateService().StartAsync(CancellationToken.None);

        var request = fixture.PollPublisher.PollRequests.Should().ContainSingle().Which;
        request.Options.Should().HaveCount(10);
        request.IsAnonymous.Should().BeFalse();
        request.AllowAddingOptions.Should().BeFalse();
        request.ShuffleOptions.Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_ShouldNormalizeTournamentTitleInPollOptions()
    {
        var fixture = new PollFixture(new[] { CreateTournament(title: "Синхронный турнир (СИНХРОН)  ") });

        await fixture.CreateService().StartAsync(CancellationToken.None);

        var request = fixture.PollPublisher.PollRequests.Should().ContainSingle().Which;
        request.Options.Should().Equal("Синхронный турнир", "посмотреть результаты");
    }

    [Fact]
    public async Task StartAsync_ShouldDeleteListAndAlertWhenPollPublicationFails()
    {
        var fixture = new PollFixture(new[] { CreateTournament() });
        fixture.PollPublisher.ThrowOnPoll = true;

        var action = () => fixture.CreateService().StartAsync(CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        fixture.PollPublisher.DeletedMessageIds.Should().Equal(101);
        fixture.ChatBotClient.Alerts.Should().ContainSingle().Which.Should().Contain("Не удалось опубликовать");
    }

    [Fact]
    public async Task StartAsync_ShouldPrioritizeForcedTournamentAndDequeueItAfterPublication()
    {
        var forcedTournament = CreateTournament(
            id: 2,
            title: "Forced",
            type: 8,
            hasRussianLanguage: false,
            hasChgkGgRating: false);
        var regularTournament = CreateTournament(id: 1, title: "Regular", difficulty: 9m);
        var fixture = new PollFixture(
            new[] { regularTournament, forcedTournament },
            forcedTournamentIds: new[] { 2 });

        var session = await fixture.CreateService().StartAsync(CancellationToken.None);

        session!.Candidates.Select(x => x.TournamentId).Should().Equal(2, 1);
        fixture.ForcedTournamentRepository.RemovedIds.Should().Equal(2);
    }

    [Fact]
    public async Task StartAsync_ShouldSkipPublicationWhenMoreThanNineForcedTournamentsAreAvailable()
    {
        var tournaments = Enumerable.Range(1, 10)
            .Select(id => CreateTournament(id, $"Forced {id}", type: 8, hasRussianLanguage: false, hasChgkGgRating: false))
            .ToArray();
        var fixture = new PollFixture(tournaments, forcedTournamentIds: Enumerable.Range(1, 10).ToArray());

        var session = await fixture.CreateService().StartAsync(CancellationToken.None);

        session.Should().BeNull();
        fixture.PollPublisher.PollRequests.Should().BeEmpty();
        fixture.ForcedTournamentRepository.RemovedIds.Should().BeEmpty();
        fixture.ChatBotClient.Alerts.Should().ContainSingle().Which.Should().Contain("лимит");
    }

    [Fact]
    public async Task StartAsync_ShouldKeepForcedTournamentQueuedWhenPublicationFails()
    {
        var fixture = new PollFixture(new[] { CreateTournament(id: 2) }, forcedTournamentIds: new[] { 2 });
        fixture.PollPublisher.ThrowOnPoll = true;

        var action = () => fixture.CreateService().StartAsync(CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        fixture.ForcedTournamentRepository.RemovedIds.Should().BeEmpty();
    }

    [Fact]
    public async Task StartAsync_ShouldReplaceStaleActivePollWhenTelegramCannotFindIt()
    {
        var activePoll = new PollSession
        {
            ChatId = -100456,
            PollMessageId = 55,
            Status = AlmatyLISPollingBot.Domain.Enums.PollLifecycleStatus.Active
        };
        var fixture = new PollFixture(new[] { CreateTournament() }, activePoll);
        fixture.PollPublisher.ThrowPollNotFoundOnStop = true;

        var session = await fixture.CreateService().StartAsync(CancellationToken.None);

        session.Should().NotBeNull();
        activePoll.Status.Should().Be(AlmatyLISPollingBot.Domain.Enums.PollLifecycleStatus.Stopped);
        activePoll.StoppedAtUtc.Should().Be(new DateTimeOffset(2026, 3, 2, 5, 0, 0, TimeSpan.Zero));
        fixture.PollPublisher.PollRequests.Should().ContainSingle();
    }

    private static TournamentDetails CreateTournament(
        int id = 7,
        string title = "Синхрон",
        decimal difficulty = 5m,
        int type = 3,
        bool hasRussianLanguage = true,
        bool hasChgkGgRating = true,
        DateOnly? targetDate = null,
        IReadOnlyList<TournamentPaymentCategory>? paymentCategories = null)
    {
        var date = targetDate ?? new DateOnly(2026, 3, 7);
        return new TournamentDetails(
            id,
            title,
            type,
            new DateTimeOffset(date.ToDateTime(new TimeOnly(12, 0)), TimeSpan.FromHours(5)),
            new DateTimeOffset(date.ToDateTime(new TimeOnly(16, 0)), TimeSpan.FromHours(5)),
            difficulty,
            hasRussianLanguage ? new[] { new TournamentLanguage("ru", "Русский") } : Array.Empty<TournamentLanguage>(),
            hasChgkGgRating ? new[] { "chgkgg" } : Array.Empty<string>(),
            new[] { new TournamentEditor("Иван", "Иванович", "Иванов") },
            new Dictionary<int, int> { [1] = 36 },
            paymentCategories ?? Array.Empty<TournamentPaymentCategory>());
    }

    private sealed class PollFixture
    {
        private readonly IReadOnlyCollection<TournamentDetails> tournaments;

        public PollFixture(
            IReadOnlyCollection<TournamentDetails> tournaments,
            PollSession? activePoll = null,
            IReadOnlyCollection<int>? forcedTournamentIds = null)
        {
            this.tournaments = tournaments;
            ForcedTournamentRepository = new StubForcedTournamentRepository(forcedTournamentIds);
            TournamentClient = new StubTournamentClient(tournaments);
            PollSessionRepository = new StubPollSessionRepository(activePoll);
        }

        public StubPollPublisher PollPublisher { get; } = new();
        public StubChatBotClient ChatBotClient { get; } = new();
        public StubForcedTournamentRepository ForcedTournamentRepository { get; }
        public StubTournamentClient TournamentClient { get; }
        public StubPollSessionRepository PollSessionRepository { get; }

        public StartPollService CreateService()
        {
            var clock = new StubClock();
            return new StartPollService(
                clock,
                ForcedTournamentRepository,
                PollSessionRepository,
                new PollCandidatePreparationService(
                    clock,
                    new StubSettingsRepository(),
                    new StubLookupRepository(),
                    ForcedTournamentRepository,
                    TournamentClient,
                    new PollCandidateSelectionService()),
                new TournamentListFormatter(new StubExchangeRateProvider()),
                PollPublisher,
                ChatBotClient);
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
            TargetChatId = -100123,
            ApplicationTimeZone = "Asia/Almaty",
            DefaultPollStopTime = new TimeOnly(21, 0)
        };

        public Task<BotSettings?> GetAsync(CancellationToken cancellationToken) => Task.FromResult<BotSettings?>(settings);

        public Task SaveAsync(BotSettings settings, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StubLookupRepository : IReadOnlyLookupRepository
    {
        public Task<IReadOnlyCollection<int>> GetExcludedTournamentIdsAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<int>>(Array.Empty<int>());

        public Task<IReadOnlyCollection<long>> GetShadowBannedUserIdsAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<long>>(Array.Empty<long>());

        public Task<IReadOnlyCollection<long>> GetAdminUserIdsAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<long>>(Array.Empty<long>());
    }

    private sealed class StubPollSessionRepository : IPollSessionRepository
    {
        private readonly PollSession? activePoll;

        public StubPollSessionRepository(PollSession? activePoll = null)
        {
            this.activePoll = activePoll;
        }

        public Task<PollSession?> GetActiveAsync(CancellationToken cancellationToken) => Task.FromResult(activePoll);

        public Task AddAsync(PollSession pollSession, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StubForcedTournamentRepository : IForcedTournamentRepository
    {
        private readonly IReadOnlyCollection<ForcedTournament> queuedTournaments;

        public StubForcedTournamentRepository(IReadOnlyCollection<int>? forcedTournamentIds = null)
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
            => Task.FromResult<IReadOnlyCollection<int>>(tournamentIds);

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
        {
            return Task.FromResult(tournaments);
        }
    }

    private sealed class StubExchangeRateProvider : IExchangeRateProvider
    {
        public Task<ExchangeRateQuote?> GetKztRateAsync(string currencyCode, CancellationToken cancellationToken)
            => Task.FromResult<ExchangeRateQuote?>(null);
    }

    private sealed class StubPollPublisher : IPollPublisher
    {
        public List<string> HtmlMessages { get; } = new();
        public List<PollPublicationRequest> PollRequests { get; } = new();
        public List<int> DeletedMessageIds { get; } = new();
        public bool ThrowOnPoll { get; set; }
        public bool ThrowPollNotFoundOnStop { get; set; }

        public Task<int> SendHtmlMessageAsync(long chatId, string message, CancellationToken cancellationToken)
        {
            HtmlMessages.Add(message);
            return Task.FromResult(101);
        }

        public Task<PublishedPoll> SendPollAsync(PollPublicationRequest request, CancellationToken cancellationToken)
        {
            PollRequests.Add(request);
            return ThrowOnPoll
                ? Task.FromException<PublishedPoll>(new InvalidOperationException("Telegram unavailable."))
                : Task.FromResult(new PublishedPoll("telegram-poll-id", 102));
        }

        public Task StopPollAsync(long chatId, int pollMessageId, CancellationToken cancellationToken)
        {
            return ThrowPollNotFoundOnStop
                ? Task.FromException(new PollNotFoundException(new InvalidOperationException()))
                : Task.CompletedTask;
        }

        public Task DeleteMessageAsync(long chatId, int messageId, CancellationToken cancellationToken)
        {
            DeletedMessageIds.Add(messageId);
            return Task.CompletedTask;
        }
    }

    private sealed class StubChatBotClient : IChatBotClient
    {
        public List<string> Alerts { get; } = new();

        public Task SendMainAdminAlertAsync(string message, CancellationToken cancellationToken)
        {
            Alerts.Add(message);
            return Task.CompletedTask;
        }
    }
}
