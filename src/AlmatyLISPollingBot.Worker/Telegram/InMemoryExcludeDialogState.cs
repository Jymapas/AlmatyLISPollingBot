using System.Collections.Concurrent;

namespace AlmatyLISPollingBot.Worker.Telegram;

public sealed class InMemoryExcludeDialogState : IExcludeDialogState
{
    private readonly ConcurrentDictionary<long, byte> awaitingUserIds = new();

    public void Start(long telegramUserId)
    {
        awaitingUserIds[telegramUserId] = 0;
    }

    public bool IsAwaitingInput(long telegramUserId)
    {
        return awaitingUserIds.ContainsKey(telegramUserId);
    }

    public bool Cancel(long telegramUserId)
    {
        return awaitingUserIds.TryRemove(telegramUserId, out _);
    }
}
