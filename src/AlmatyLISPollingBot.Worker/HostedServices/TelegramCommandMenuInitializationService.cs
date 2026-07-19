using AlmatyLISPollingBot.Application.Contracts.Bot;
using AlmatyLISPollingBot.Application.Abstractions.Administrators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AlmatyLISPollingBot.Worker.HostedServices;

public sealed class TelegramCommandMenuInitializationService : IHostedService
{
    internal static readonly BotCommand[] AdministratorCommands =
    {
        new(BotCommands.Poll, "Создать опрос"),
        new(BotCommands.Stop, "Остановить опрос")
    };

    internal static readonly BotCommand[] PrivateAdministratorCommands =
    {
        new(BotCommands.Poll, "Создать опрос"),
        new(BotCommands.Stop, "Остановить опрос"),
        new(BotCommands.Preview, "Предпросмотр опроса"),
        new(BotCommands.Options, "Показать турниры"),
        new(BotCommands.Exclude, "Исключить турниры"),
        new(BotCommands.Excluded, "Показать исключённые турниры"),
        new(BotCommands.Unexclude, "Вернуть турниры в пул"),
        new(BotCommands.Force, "Добавить синхрон в опрос"),
        new(BotCommands.Cancel, "Отменить диалог"),
        new(BotCommands.MakePost, "Сформировать пост")
    };

    private readonly ITelegramBotClient botClient;
    private readonly IOptions<BotConfiguration> botConfiguration;
    private readonly ILogger<TelegramCommandMenuInitializationService> logger;
    private readonly IServiceScopeFactory serviceScopeFactory;

    public TelegramCommandMenuInitializationService(
        ITelegramBotClient botClient,
        IOptions<BotConfiguration> botConfiguration,
        ILogger<TelegramCommandMenuInitializationService> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        this.botClient = botClient;
        this.botConfiguration = botConfiguration;
        this.logger = logger;
        this.serviceScopeFactory = serviceScopeFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await ConfigureAsync(cancellationToken);
    }

    public async Task ConfigureAsync(CancellationToken cancellationToken)
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

        using var scope = serviceScopeFactory.CreateScope();
        var administratorClient = scope.ServiceProvider.GetRequiredService<IChatAdministratorClient>();
        var administrators = await administratorClient.GetAdministratorsAsync(targetChatId, cancellationToken);

        foreach (var administrator in administrators
                     .Select(x => x.TelegramUserId)
                     .Distinct())
        {
            await botClient.SetMyCommands(
                PrivateAdministratorCommands,
                scope: new BotCommandScopeChat { ChatId = administrator },
                cancellationToken: cancellationToken);
        }

        logger.LogInformation(
            "Telegram command menus configured for target chat administrators and {PrivateAdministratorCount} private chats.",
            administrators.Select(x => x.TelegramUserId).Distinct().Count());
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
