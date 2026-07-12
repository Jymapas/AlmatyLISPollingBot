using AlmatyLISPollingBot.Application.Contracts.ExchangeRates;

namespace AlmatyLISPollingBot.Application.Abstractions.ExchangeRates;

public interface IExchangeRateProvider
{
    Task<ExchangeRateQuote?> GetKztRateAsync(string currencyCode, CancellationToken cancellationToken);
}
