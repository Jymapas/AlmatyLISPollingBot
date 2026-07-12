using AlmatyLISPollingBot.Application.Abstractions.Administrators;
using AlmatyLISPollingBot.Application.Contracts.Administrators;
using Telegram.Bot;

namespace AlmatyLISPollingBot.Worker.Telegram;

public sealed class TelegramChatAdministratorClient : IChatAdministratorClient
{
    private readonly ITelegramBotClient botClient;

    public TelegramChatAdministratorClient(ITelegramBotClient botClient)
    {
        this.botClient = botClient;
    }

    public async Task<IReadOnlyCollection<ChatAdministratorInfo>> GetAdministratorsAsync(
        long chatId,
        CancellationToken cancellationToken)
    {
        var administrators = await botClient.GetChatAdministrators(chatId, cancellationToken: cancellationToken);
        return administrators
            .Select(x => new ChatAdministratorInfo(x.User.Id, x.User.Username))
            .ToArray();
    }
}
