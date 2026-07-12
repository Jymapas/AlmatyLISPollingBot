using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlmatyLISPollingBot.Infrastructure.Persistence.Repositories;

public sealed class CurrencyExchangeRateRepository : ICurrencyExchangeRateRepository
{
    private readonly BotDbContext dbContext;

    public CurrencyExchangeRateRepository(BotDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<CurrencyExchangeRate?> GetAsync(string currencyCode, CancellationToken cancellationToken)
    {
        return dbContext.CurrencyExchangeRates.SingleOrDefaultAsync(
            x => x.CurrencyCode == currencyCode,
            cancellationToken);
    }

    public async Task SaveAsync(CurrencyExchangeRate exchangeRate, CancellationToken cancellationToken)
    {
        var existing = await GetAsync(exchangeRate.CurrencyCode, cancellationToken);
        if (existing is null)
        {
            await dbContext.CurrencyExchangeRates.AddAsync(exchangeRate, cancellationToken);
        }
        else
        {
            existing.TengePerNominal = exchangeRate.TengePerNominal;
            existing.Nominal = exchangeRate.Nominal;
            existing.EffectiveDate = exchangeRate.EffectiveDate;
            existing.UpdatedAtUtc = exchangeRate.UpdatedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
