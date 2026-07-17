using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Contracts.Bot;
using AlmatyLISPollingBot.Domain.Entities;

namespace AlmatyLISPollingBot.Application.Features.Administrators;

public sealed class BotSettingsSyncService
{
    private readonly IBotSettingsRepository settingsRepository;

    public BotSettingsSyncService(IBotSettingsRepository settingsRepository)
    {
        this.settingsRepository = settingsRepository;
    }

    public async Task ExecuteAsync(BotConfiguration configuration, CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken)
            ?? new BotSettings();
        settings.TargetChatId = configuration.TargetChatId;
        settings.MainAdminUserId = configuration.MainAdminUserId;
        settings.ApplicationTimeZone = configuration.ApplicationTimeZone;
        settings.DefaultPollStopTime = TimeOnly.FromTimeSpan(configuration.DefaultPollStopTime);
        settings.Venue = configuration.DefaultVenue;

        await settingsRepository.SaveAsync(settings, cancellationToken);
    }
}
