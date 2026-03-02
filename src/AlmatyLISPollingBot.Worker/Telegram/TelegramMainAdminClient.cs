using AlmatyLISPollingBot.Application.Abstractions.Messaging;
using AlmatyLISPollingBot.Application.Contracts.Bot;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace AlmatyLISPollingBot.Worker.Telegram;

public sealed class TelegramMainAdminClient : IChatBotClient
{
    private readonly ITelegramBotClient botClient;
    private readonly IOptions<BotConfiguration> botConfiguration;

    public TelegramMainAdminClient(
        ITelegramBotClient botClient,
        IOptions<BotConfiguration> botConfiguration)
    {
        this.botClient = botClient;
        this.botConfiguration = botConfiguration;
    }

    public Task SendMainAdminAlertAsync(string message, CancellationToken cancellationToken)
    {
        return botClient.SendMessage(
            chatId: botConfiguration.Value.MainAdminUserId,
            text: message,
            cancellationToken: cancellationToken);
    }
}
