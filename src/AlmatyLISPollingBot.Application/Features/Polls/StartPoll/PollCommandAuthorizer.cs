using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Contracts.Polls;

namespace AlmatyLISPollingBot.Application.Features.Polls.StartPoll;

public sealed class PollCommandAuthorizer
{
    private readonly IBotSettingsRepository settingsRepository;
    private readonly IReadOnlyLookupRepository lookupRepository;

    public PollCommandAuthorizer(
        IBotSettingsRepository settingsRepository,
        IReadOnlyLookupRepository lookupRepository)
    {
        this.settingsRepository = settingsRepository;
        this.lookupRepository = lookupRepository;
    }

    public async Task<bool> IsAuthorizedAsync(PollCommandContext context, CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken);
        if (settings is null)
        {
            return false;
        }

        var sourceIsAllowed = context.IsPrivateChat || context.SourceChatId == settings.TargetChatId;
        if (!sourceIsAllowed)
        {
            return false;
        }

        var administratorIds = await lookupRepository.GetAdminUserIdsAsync(cancellationToken);
        return administratorIds.Contains(context.UserId);
    }
}
