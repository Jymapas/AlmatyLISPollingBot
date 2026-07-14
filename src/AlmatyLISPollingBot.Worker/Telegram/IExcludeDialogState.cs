namespace AlmatyLISPollingBot.Worker.Telegram;

public interface IExcludeDialogState
{
    void Start(long telegramUserId);
    bool IsAwaitingInput(long telegramUserId);
    bool Cancel(long telegramUserId);
}
