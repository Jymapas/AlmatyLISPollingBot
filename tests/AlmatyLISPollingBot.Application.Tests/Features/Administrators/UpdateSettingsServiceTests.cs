using AlmatyLISPollingBot.Application.Abstractions.Administrators;
using AlmatyLISPollingBot.Application.Abstractions.Clock;
using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Contracts.Administrators;
using AlmatyLISPollingBot.Application.Contracts.Bot;
using AlmatyLISPollingBot.Application.Features.Administrators;
using AlmatyLISPollingBot.Domain.Entities;
using FluentAssertions;

namespace AlmatyLISPollingBot.Application.Tests.Features.Administrators;

public sealed class UpdateSettingsServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldSynchronizeSettingsAndReplaceAdministrators()
    {
        var existingSettings = new BotSettings
        {
            TargetChatId = -100001,
            MainAdminUserId = 1,
            ApplicationTimeZone = "UTC",
            DefaultPollStopTime = new TimeOnly(10, 0),
            Venue = "Old venue"
        };
        var settingsRepository = new StubSettingsRepository(existingSettings);
        var administratorRepository = new StubAdministratorRepository();
        var administratorClient = new StubAdministratorClient(
            new[]
            {
                new ChatAdministratorInfo(10, "admin"),
                new ChatAdministratorInfo(10, "duplicate"),
                new ChatAdministratorInfo(20, null)
            });
        var configuration = new BotConfiguration
        {
            TargetChatId = -100123,
            MainAdminUserId = 42,
            ApplicationTimeZone = "Asia/Almaty",
            DefaultPollStopTime = TimeSpan.FromHours(21),
            DefaultVenue = "New venue"
        };
        var sut = new UpdateSettingsService(
            new BotSettingsSyncService(settingsRepository),
            new AdminSyncService(administratorClient, administratorRepository, new StubClock()));

        await sut.ExecuteAsync(configuration, CancellationToken.None);

        settingsRepository.SavedSettings.Should().BeSameAs(existingSettings);
        existingSettings.TargetChatId.Should().Be(configuration.TargetChatId);
        existingSettings.MainAdminUserId.Should().Be(configuration.MainAdminUserId);
        existingSettings.ApplicationTimeZone.Should().Be(configuration.ApplicationTimeZone);
        existingSettings.DefaultPollStopTime.Should().Be(new TimeOnly(21, 0));
        existingSettings.Venue.Should().Be(configuration.DefaultVenue);
        administratorClient.RequestedChatIds.Should().Equal(configuration.TargetChatId);
        administratorRepository.ReplacedAdministrators.Select(x => x.TelegramUserId).Should().Equal(10, 20);
    }

    private sealed class StubClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 7, 15, 5, 0, 0, TimeSpan.Zero);
    }

    private sealed class StubSettingsRepository : IBotSettingsRepository
    {
        private readonly BotSettings settings;

        public StubSettingsRepository(BotSettings settings)
        {
            this.settings = settings;
        }

        public BotSettings? SavedSettings { get; private set; }

        public Task<BotSettings?> GetAsync(CancellationToken cancellationToken) => Task.FromResult<BotSettings?>(settings);

        public Task SaveAsync(BotSettings settings, CancellationToken cancellationToken)
        {
            SavedSettings = settings;
            return Task.CompletedTask;
        }
    }

    private sealed class StubAdministratorRepository : IChatAdministratorRepository
    {
        public IReadOnlyCollection<ChatAdministrator> ReplacedAdministrators { get; private set; } = Array.Empty<ChatAdministrator>();

        public Task ReplaceAsync(IReadOnlyCollection<ChatAdministrator> administrators, CancellationToken cancellationToken)
        {
            ReplacedAdministrators = administrators;
            return Task.CompletedTask;
        }
    }

    private sealed class StubAdministratorClient : IChatAdministratorClient
    {
        private readonly IReadOnlyCollection<ChatAdministratorInfo> administrators;

        public StubAdministratorClient(IReadOnlyCollection<ChatAdministratorInfo> administrators)
        {
            this.administrators = administrators;
        }

        public List<long> RequestedChatIds { get; } = new();

        public Task<IReadOnlyCollection<ChatAdministratorInfo>> GetAdministratorsAsync(
            long chatId,
            CancellationToken cancellationToken)
        {
            RequestedChatIds.Add(chatId);
            return Task.FromResult(administrators);
        }
    }
}
