using AlmatyLISPollingBot.Application.Abstractions.Administrators;
using AlmatyLISPollingBot.Application.Abstractions.Clock;
using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Contracts.Administrators;
using AlmatyLISPollingBot.Application.Features.Administrators;
using AlmatyLISPollingBot.Domain.Entities;
using FluentAssertions;

namespace AlmatyLISPollingBot.Application.Tests.Features.Administrators;

public sealed class AdminSyncServiceTests
{
    [Fact]
    public async Task SynchronizeAsync_ShouldDeduplicateAndReplaceCachedAdministrators()
    {
        var client = new StubChatAdministratorClient(
            new[]
            {
                new ChatAdministratorInfo(1, "first"),
                new ChatAdministratorInfo(1, "duplicate"),
                new ChatAdministratorInfo(2, null)
            });
        var repository = new StubChatAdministratorRepository();
        var sut = new AdminSyncService(client, repository, new StubClock());

        await sut.SynchronizeAsync(-100123L, CancellationToken.None);

        repository.ReplacedAdministrators.Select(x => x.TelegramUserId).Should().Equal(1, 2);
        repository.ReplacedAdministrators.Single(x => x.TelegramUserId == 1).Username.Should().Be("first");
        repository.ReplacedAdministrators.Single(x => x.TelegramUserId == 2).Username.Should().BeNull();
        repository.ReplacedAdministrators.Should().OnlyContain(x =>
            x.SyncedAtUtc == new DateTimeOffset(2026, 3, 2, 5, 0, 0, TimeSpan.Zero));
        client.RequestedChatIds.Should().Equal(-100123L);
    }

    private sealed class StubClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 3, 2, 5, 0, 0, TimeSpan.Zero);
    }

    private sealed class StubChatAdministratorClient : IChatAdministratorClient
    {
        private readonly IReadOnlyCollection<ChatAdministratorInfo> administrators;

        public StubChatAdministratorClient(IReadOnlyCollection<ChatAdministratorInfo> administrators)
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

    private sealed class StubChatAdministratorRepository : IChatAdministratorRepository
    {
        public IReadOnlyCollection<ChatAdministrator> ReplacedAdministrators { get; private set; } = Array.Empty<ChatAdministrator>();

        public Task ReplaceAsync(IReadOnlyCollection<ChatAdministrator> administrators, CancellationToken cancellationToken)
        {
            ReplacedAdministrators = administrators;
            return Task.CompletedTask;
        }
    }
}
