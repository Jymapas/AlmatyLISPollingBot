using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using AlmatyLISPollingBot.Worker.Telegram;

namespace AlmatyLISPollingBot.Worker.HostedServices;

public sealed class TelegramLongPollingService : BackgroundService
{
    private readonly ITelegramBotClient botClient;
    private readonly TelegramUpdateRouter updateRouter;
    private readonly ILogger<TelegramLongPollingService> logger;

    public TelegramLongPollingService(
        ITelegramBotClient botClient,
        TelegramUpdateRouter updateRouter,
        ILogger<TelegramLongPollingService> logger)
    {
        this.botClient = botClient;
        this.updateRouter = updateRouter;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<global::Telegram.Bot.Types.Enums.UpdateType>()
        };

        botClient.StartReceiving(
            updateHandler: async (_, update, cancellationToken) => await updateRouter.RouteAsync(update, cancellationToken),
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);

        logger.LogInformation("Telegram long polling started.");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Telegram polling error.");
        return Task.CompletedTask;
    }
}
