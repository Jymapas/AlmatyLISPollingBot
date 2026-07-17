using AlmatyLISPollingBot.Application.Contracts.Bot;

namespace AlmatyLISPollingBot.Application.Features.Administrators;

public sealed class UpdateSettingsService
{
    private readonly BotSettingsSyncService botSettingsSyncService;
    private readonly AdminSyncService adminSyncService;

    public UpdateSettingsService(
        BotSettingsSyncService botSettingsSyncService,
        AdminSyncService adminSyncService)
    {
        this.botSettingsSyncService = botSettingsSyncService;
        this.adminSyncService = adminSyncService;
    }

    public async Task ExecuteAsync(BotConfiguration configuration, CancellationToken cancellationToken)
    {
        await botSettingsSyncService.ExecuteAsync(configuration, cancellationToken);
        await adminSyncService.SynchronizeAsync(configuration.TargetChatId, cancellationToken);
    }
}
