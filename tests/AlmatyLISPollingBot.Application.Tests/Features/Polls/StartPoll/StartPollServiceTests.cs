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
        var fixture = new PollFixture(new[] { CreateTournament() });

        var session = await fixture.CreateService().StartAsync(CancellationToken.None);

        session.Should().NotBeNull();
        session!.Status.Should().Be(AlmatyLISPollingBot.Domain.Enums.PollLifecycleStatus.Active);
        session.ListMessageId.Should().Be(101);
        session.PollMessageId.Should().Be(102);
        session.TelegramPollId.Should().Be("telegram-poll-id");
        session.Candidates.Should().ContainSingle();
        session.Candidates[0].IsAvailableAtFirstSlot.Should().BeTrue();
        session.Candidates[0].IsAvailableAtSecondSlot.Should().BeTrue();

        fixture.PollPublisher.HtmlMessages.Should().ContainSingle();
        fixture.PollPublisher.PollRequests.Should().ContainSingle();
        var request = fixture.PollPublisher.PollRequests[0];
        request.Question.Should().Be("Выбираем 2 синхрона на субботу, 07.03.2026:");
        request.Options.Should().Equal("Синхрон", "посмотреть результаты");
        request.IsAnonymous.Should().BeFalse();
        request.AllowsMultipleAnswers.Should().BeTrue();
        request.ShuffleOptions.Should().BeFalse();
        request.AllowAddingOptions.Should().BeTrue();
        fixture.ChatBotClient.Alerts.Should().BeEmpty();
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
    public async Task StartAsync_ShouldDeleteListAndAlertWhenPollPublicationFails()
    {
        var fixture = new PollFixture(new[] { CreateTournament() });
        fixture.PollPublisher.ThrowOnPoll = true;

        var action = () => fixture.CreateService().StartAsync(CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        fixture.PollPublisher.DeletedMessageIds.Should().Equal(101);
        fixture.ChatBotClient.Alerts.Should().ContainSingle().Which.Should().Contain("Не удалось опубликовать");
    }

    private static TournamentDetails CreateTournament(int id = 7, string title = "Синхрон", decimal difficulty = 5m)
    {
        return new TournamentDetails(
            id,
            title,
            3,
            new DateTimeOffset(2026, 3, 7, 12, 0, 0, TimeSpan.FromHours(5)),
            new DateTimeOffset(2026, 3, 7, 16, 0, 0, TimeSpan.FromHours(5)),
            difficulty,
            new[] { new TournamentLanguage("ru", "Русский") },
            new[] { "chgkgg" },
            new[] { new TournamentEditor("Иван", "Иванович", "Иванов") },
            new Dictionary<int, int> { [1] = 36 },
            Array.Empty<TournamentPaymentCategory>());
    }

    private sealed class PollFixture
    {
        private readonly IReadOnlyCollection<TournamentDetails> tournaments;

        public PollFixture(IReadOnlyCollection<TournamentDetails> tournaments)
        {
            this.tournaments = tournaments;
        }

        public StubPollPublisher PollPublisher { get; } = new();
        public StubChatBotClient ChatBotClient { get; } = new();

        public StartPollService CreateService()
        {
            return new StartPollService(
                new StubClock(),
                new StubSettingsRepository(),
                new StubLookupRepository(),
                new StubPollSessionRepository(),
                new StubTournamentClient(tournaments),
                new PollCandidateSelectionService(),
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
        public Task<PollSession?> GetActiveAsync(CancellationToken cancellationToken) => Task.FromResult<PollSession?>(null);

        public Task AddAsync(PollSession pollSession, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
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

    private sealed class StubPollPublisher : IPollPublisher
    {
        public List<string> HtmlMessages { get; } = new();
        public List<PollPublicationRequest> PollRequests { get; } = new();
        public List<int> DeletedMessageIds { get; } = new();
        public bool ThrowOnPoll { get; set; }

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

        public Task StopPollAsync(long chatId, int pollMessageId, CancellationToken cancellationToken) => Task.CompletedTask;

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
