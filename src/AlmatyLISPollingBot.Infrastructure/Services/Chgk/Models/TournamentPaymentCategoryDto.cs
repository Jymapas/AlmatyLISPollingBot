namespace AlmatyLISPollingBot.Infrastructure.Services.Chgk.Models;

internal sealed class TournamentPaymentCategoryDto
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}
