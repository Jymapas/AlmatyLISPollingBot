using System.Collections.Concurrent;

namespace AlmatyLISPollingBot.Worker.Telegram;

public sealed class InMemoryPrivateAdminDialogState : IPrivateAdminDialogState
{
    private readonly ConcurrentDictionary<long, PrivateAdminDialogKind> activeDialogs = new();

    public void Start(long telegramUserId, PrivateAdminDialogKind dialogKind)
    {
        activeDialogs[telegramUserId] = dialogKind;
    }

    public PrivateAdminDialogKind? GetActive(long telegramUserId)
    {
        return activeDialogs.TryGetValue(telegramUserId, out var dialogKind)
            ? dialogKind
            : null;
    }

    public PrivateAdminDialogKind? Cancel(long telegramUserId)
    {
        return activeDialogs.TryRemove(telegramUserId, out var dialogKind)
            ? dialogKind
            : null;
    }
}
