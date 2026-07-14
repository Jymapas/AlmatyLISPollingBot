namespace AlmatyLISPollingBot.Application.Contracts.Polls;

public sealed record PollCommandContext(long SourceChatId, long UserId, bool IsPrivateChat);
