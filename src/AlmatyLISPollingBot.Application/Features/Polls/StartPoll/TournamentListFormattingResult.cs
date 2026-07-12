namespace AlmatyLISPollingBot.Application.Features.Polls.StartPoll;

public sealed record TournamentListFormattingResult(
    IReadOnlyList<string> Pages,
    bool HasUnconvertedPrices);
