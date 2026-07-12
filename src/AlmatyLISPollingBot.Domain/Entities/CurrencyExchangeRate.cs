using AlmatyLISPollingBot.Domain.Common;

namespace AlmatyLISPollingBot.Domain.Entities;

public sealed class CurrencyExchangeRate : Entity
{
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal TengePerNominal { get; set; }
    public int Nominal { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
