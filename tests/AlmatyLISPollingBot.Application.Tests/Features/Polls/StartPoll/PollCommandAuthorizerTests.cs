using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Contracts.Polls;
using AlmatyLISPollingBot.Application.Features.Polls.StartPoll;
using AlmatyLISPollingBot.Domain.Entities;
using FluentAssertions;

namespace AlmatyLISPollingBot.Application.Tests.Features.Polls.StartPoll;

public sealed class PollCommandAuthorizerTests
{
    [Theory]
    [InlineData(-100123L, 7L, false, true)]
    [InlineData(7L, 7L, true, true)]
    [InlineData(-100555L, 7L, false, false)]
    [InlineData(-100123L, 99L, false, false)]
    public async Task IsAuthorizedAsync_ShouldAllowOnlyCachedAdminFromTargetChatOrPrivateChat(
        long sourceChatId,
        long userId,
        bool isPrivateChat,
        bool expected)
    {
        var sut = new PollCommandAuthorizer(
            new StubSettingsRepository(),
            new StubLookupRepository(new[] { 7L }));

        var result = await sut.IsAuthorizedAsync(
            new PollCommandContext(sourceChatId, userId, isPrivateChat),
            CancellationToken.None);

        result.Should().Be(expected);
    }

    private sealed class StubSettingsRepository : IBotSettingsRepository
    {
        public Task<BotSettings?> GetAsync(CancellationToken cancellationToken)
            => Task.FromResult<BotSettings?>(new BotSettings { TargetChatId = -100123L });

        public Task SaveAsync(BotSettings settings, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StubLookupRepository : IReadOnlyLookupRepository
    {
        private readonly IReadOnlyCollection<long> administratorIds;

        public StubLookupRepository(IReadOnlyCollection<long> administratorIds)
        {
            this.administratorIds = administratorIds;
        }

        public Task<IReadOnlyCollection<int>> GetExcludedTournamentIdsAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<int>>(Array.Empty<int>());

        public Task<IReadOnlyCollection<long>> GetShadowBannedUserIdsAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<long>>(Array.Empty<long>());

        public Task<IReadOnlyCollection<long>> GetAdminUserIdsAsync(CancellationToken cancellationToken)
            => Task.FromResult(administratorIds);
    }
}
