using AlmatyLISPollingBot.Domain.Entities;

namespace AlmatyLISPollingBot.Application.Abstractions.Persistence;

public interface ICurrencyExchangeRateRepository
{
    Task<CurrencyExchangeRate?> GetAsync(string currencyCode, CancellationToken cancellationToken);
    Task SaveAsync(CurrencyExchangeRate exchangeRate, CancellationToken cancellationToken);
}
