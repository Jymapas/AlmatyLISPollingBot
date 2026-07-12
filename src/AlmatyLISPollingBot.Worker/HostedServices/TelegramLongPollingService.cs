using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using AlmatyLISPollingBot.Worker.Telegram;

namespace AlmatyLISPollingBot.Worker.HostedServices;

public sealed class TelegramLongPollingService : BackgroundService
{
    private readonly ITelegramBotClient botClient;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<TelegramLongPollingService> logger;

    public TelegramLongPollingService(
        ITelegramBotClient botClient,
        IServiceScopeFactory scopeFactory,
        ILogger<TelegramLongPollingService> logger)
    {
        this.botClient = botClient;
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<global::Telegram.Bot.Types.Enums.UpdateType>()
        };

        botClient.StartReceiving(
            updateHandler: RouteUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);

        logger.LogInformation("Telegram long polling started.");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task RouteUpdateAsync(
        ITelegramBotClient _,
        Update update,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var updateRouter = scope.ServiceProvider.GetRequiredService<TelegramUpdateRouter>();
        await updateRouter.RouteAsync(update, cancellationToken);
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Telegram polling error.");
        return Task.CompletedTask;
    }
}
