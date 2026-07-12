namespace AlmatyLISPollingBot.Application.Contracts.ExchangeRates;

public sealed record ExchangeRateQuote(decimal TengePerNominal, int Nominal, DateOnly EffectiveDate)
{
    public decimal ConvertToTenge(decimal amount)
    {
        return Math.Round(amount * TengePerNominal / Nominal, 0, MidpointRounding.AwayFromZero);
    }
}
