using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using AlmatyLISPollingBot.Application.Contracts.Bot;

namespace AlmatyLISPollingBot.Infrastructure.Persistence;

public sealed class BotDbContextFactory : IDesignTimeDbContextFactory<BotDbContext>
{
    public BotDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BotDbContext>();
        var configuration = new DatabaseConfiguration
        {
            Host = Environment.GetEnvironmentVariable("DATABASE__HOST") ?? "localhost",
            Port = int.TryParse(Environment.GetEnvironmentVariable("DATABASE__PORT"), out var port) ? port : 5432,
            Name = Environment.GetEnvironmentVariable("DATABASE__NAME") ?? "almaty_lis_polling_bot",
            Username = Environment.GetEnvironmentVariable("DATABASE__USERNAME") ?? "postgres",
            Password = Environment.GetEnvironmentVariable("DATABASE__PASSWORD") ?? "postgres"
        };
        var connectionString = PostgresConnectionStringFactory.Build(configuration);

        optionsBuilder.UseNpgsql(connectionString);
        return new BotDbContext(optionsBuilder.Options);
    }
}
