namespace AlmatyLISPollingBot.Application.Features.Polls.Options;

public sealed record TournamentOptionsResult(
    DateOnly TargetDate,
    IReadOnlyList<string> Pages);
