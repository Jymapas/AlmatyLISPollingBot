using AlmatyLISPollingBot.Application.Abstractions.Clock;
using AlmatyLISPollingBot.Application.Abstractions.ExchangeRates;
using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Abstractions.Scheduling;
using AlmatyLISPollingBot.Application.Abstractions.Tournaments;
using AlmatyLISPollingBot.Application.Contracts.Bot;
using AlmatyLISPollingBot.Infrastructure.Persistence;
using AlmatyLISPollingBot.Infrastructure.Persistence.Repositories;
using AlmatyLISPollingBot.Infrastructure.Scheduling;
using AlmatyLISPollingBot.Infrastructure.Services;
using AlmatyLISPollingBot.Infrastructure.Services.ExchangeRates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace AlmatyLISPollingBot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseConfiguration>()
            .Bind(configuration.GetSection(DatabaseConfiguration.SectionName))
            .Validate(x => !string.IsNullOrWhiteSpace(x.Host), "Database host is required.")
            .Validate(x => x.Port > 0, "Database port must be greater than zero.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.Name), "Database name is required.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.Username), "Database username is required.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.Password), "Database password is required.")
            .ValidateOnStart();

        services.AddOptions<ChgkApiConfiguration>()
            .Bind(configuration.GetSection(ChgkApiConfiguration.SectionName))
            .Validate(x => IsHttpsUri(x.BaseUrl), "Valid HTTPS CHGK API base url is required.")
            .ValidateOnStart();

        services.AddOptions<NationalBankConfiguration>()
            .Bind(configuration.GetSection(NationalBankConfiguration.SectionName))
            .Validate(x => IsHttpsUri(x.BaseUrl), "Valid HTTPS National Bank base url is required.")
            .ValidateOnStart();

        services.AddDbContext<BotDbContext>((serviceProvider, options) =>
        {
            var databaseConfiguration = serviceProvider
                .GetRequiredService<IOptions<DatabaseConfiguration>>()
                .Value;
            var connectionString = PostgresConnectionStringFactory.Build(databaseConfiguration);
            options.UseNpgsql(connectionString);
        });
        services.AddScoped<IBotSettingsRepository, BotSettingsRepository>();
        services.AddScoped<IPollSessionRepository, PollSessionRepository>();
        services.AddScoped<IReadOnlyLookupRepository, LookupRepository>();
        services.AddScoped<ICurrencyExchangeRateRepository, CurrencyExchangeRateRepository>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IBackgroundJobScheduler, NoOpBackgroundJobScheduler>();

        services.AddHttpClient<IChgkTournamentClient, ChgkTournamentClient>((serviceProvider, client) =>
            {
                var apiConfiguration = serviceProvider
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<ChgkApiConfiguration>>()
                    .Value;
                client.BaseAddress = new Uri(apiConfiguration.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
            });

        services.AddHttpClient<IExchangeRateProvider, NationalBankExchangeRateProvider>((serviceProvider, client) =>
            {
                var nationalBankConfiguration = serviceProvider
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<NationalBankConfiguration>>()
                    .Value;
                client.BaseAddress = new Uri(nationalBankConfiguration.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
            });

        services.AddHealthChecks()
            .AddDbContextCheck<BotDbContext>("postgres", failureStatus: HealthStatus.Unhealthy);

        return services;
    }

    private static bool IsHttpsUri(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }
}
