namespace AlmatyLISPollingBot.Application.Abstractions.Messaging;

public interface IChatBotClient
{
    Task SendMainAdminAlertAsync(string message, CancellationToken cancellationToken);
}
