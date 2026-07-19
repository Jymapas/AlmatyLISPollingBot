namespace AlmatyLISPollingBot.Application.Contracts.Polls;

public sealed record PollSnapshot(string TelegramPollId, IReadOnlyList<PollOptionSnapshot> Options);
