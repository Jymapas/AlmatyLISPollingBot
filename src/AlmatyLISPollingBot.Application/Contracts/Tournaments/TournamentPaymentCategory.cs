namespace AlmatyLISPollingBot.Application.Contracts.Tournaments;

public sealed record TournamentPaymentCategory(decimal Amount, string Currency, string Reason);
