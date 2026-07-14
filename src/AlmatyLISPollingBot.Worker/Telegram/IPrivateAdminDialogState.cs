namespace AlmatyLISPollingBot.Worker.Telegram;

public interface IPrivateAdminDialogState
{
    void Start(long telegramUserId, PrivateAdminDialogKind dialogKind);
    PrivateAdminDialogKind? GetActive(long telegramUserId);
    PrivateAdminDialogKind? Cancel(long telegramUserId);
}
