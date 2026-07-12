using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using AlmatyLISPollingBot.Application.Abstractions.Clock;
using AlmatyLISPollingBot.Application.Abstractions.ExchangeRates;
using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Contracts.ExchangeRates;
using AlmatyLISPollingBot.Domain.Entities;

namespace AlmatyLISPollingBot.Infrastructure.Services.ExchangeRates;

public sealed class NationalBankExchangeRateProvider : IExchangeRateProvider
{
    private const string RatesRoute = "rss/rates_all.xml";

    private readonly HttpClient httpClient;
    private readonly ICurrencyExchangeRateRepository exchangeRateRepository;
    private readonly IClock clock;

    public NationalBankExchangeRateProvider(
        HttpClient httpClient,
        ICurrencyExchangeRateRepository exchangeRateRepository,
        IClock clock)
    {
        this.httpClient = httpClient;
        this.exchangeRateRepository = exchangeRateRepository;
        this.clock = clock;
    }

    public async Task<ExchangeRateQuote?> GetKztRateAsync(string currencyCode, CancellationToken cancellationToken)
    {
        var normalizedCurrencyCode = currencyCode.Trim().ToUpperInvariant();
        if (string.Equals(normalizedCurrencyCode, "KZT", StringComparison.Ordinal))
        {
            return new ExchangeRateQuote(1m, 1, DateOnly.FromDateTime(clock.UtcNow.UtcDateTime));
        }

        try
        {
            var quote = await GetFreshRateAsync(normalizedCurrencyCode, cancellationToken);
            if (quote is not null)
            {
                await exchangeRateRepository.SaveAsync(
                    new CurrencyExchangeRate
                    {
                        CurrencyCode = normalizedCurrencyCode,
                        TengePerNominal = quote.TengePerNominal,
                        Nominal = quote.Nominal,
                        EffectiveDate = quote.EffectiveDate,
                        UpdatedAtUtc = clock.UtcNow
                    },
                    cancellationToken);

                return quote;
            }
        }
        catch (HttpRequestException)
        {
        }
        catch (XmlException)
        {
        }

        var cachedRate = await exchangeRateRepository.GetAsync(normalizedCurrencyCode, cancellationToken);
        return cachedRate is null
            ? null
            : new ExchangeRateQuote(cachedRate.TengePerNominal, cachedRate.Nominal, cachedRate.EffectiveDate);
    }

    private async Task<ExchangeRateQuote?> GetFreshRateAsync(string currencyCode, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(RatesRoute, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"National Bank API returned HTTP {(int)response.StatusCode}.",
                null,
                response.StatusCode);
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var document = await XDocument.LoadAsync(responseStream, LoadOptions.None, cancellationToken);
        var item = document
            .Descendants("item")
            .SingleOrDefault(x => string.Equals(x.Element("title")?.Value, currencyCode, StringComparison.OrdinalIgnoreCase));

        if (item is null
            || !decimal.TryParse(item.Element("description")?.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate)
            || !int.TryParse(item.Element("quant")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var nominal)
            || !DateOnly.TryParseExact(item.Element("pubDate")?.Value, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var effectiveDate)
            || rate <= 0m
            || nominal <= 0)
        {
            return null;
        }

        return new ExchangeRateQuote(rate, nominal, effectiveDate);
    }
}
