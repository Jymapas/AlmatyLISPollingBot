namespace AlmatyLISPollingBot.Application.Contracts.Polls;

public sealed record PollOptionSnapshot(string PersistentId, string Text, int Position, int VoterCount);
