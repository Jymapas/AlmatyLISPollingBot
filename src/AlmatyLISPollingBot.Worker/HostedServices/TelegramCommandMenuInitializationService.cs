using AlmatyLISPollingBot.Application.Contracts.Bot;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AlmatyLISPollingBot.Worker.HostedServices;

public sealed class TelegramCommandMenuInitializationService : IHostedService
{
    private static readonly BotCommand[] AdministratorCommands =
    {
        new(BotCommands.Poll, "Создать опрос")
    };

    private readonly ITelegramBotClient botClient;
    private readonly IOptions<BotConfiguration> botConfiguration;
    private readonly ILogger<TelegramCommandMenuInitializationService> logger;

    public TelegramCommandMenuInitializationService(
        ITelegramBotClient botClient,
        IOptions<BotConfiguration> botConfiguration,
        ILogger<TelegramCommandMenuInitializationService> logger)
    {
        this.botClient = botClient;
        this.botConfiguration = botConfiguration;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var targetChatId = botConfiguration.Value.TargetChatId;

        await botClient.DeleteMyCommands(scope: new BotCommandScopeDefault(), cancellationToken: cancellationToken);
        await botClient.DeleteMyCommands(
            scope: new BotCommandScopeChat { ChatId = targetChatId },
            cancellationToken: cancellationToken);
        await botClient.SetMyCommands(
            AdministratorCommands,
            scope: new BotCommandScopeChatAdministrators { ChatId = targetChatId },
            cancellationToken: cancellationToken);

        logger.LogInformation("Telegram command menu configured for target chat administrators.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
