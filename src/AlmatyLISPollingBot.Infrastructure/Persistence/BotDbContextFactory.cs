using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AlmatyLISPollingBot.Infrastructure.Persistence;

public sealed class BotDbContextFactory : IDesignTimeDbContextFactory<BotDbContext>
{
    public BotDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BotDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("DATABASE__CONNECTIONSTRING")
            ?? "Host=localhost;Port=5432;Database=almaty_lis_polling_bot;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);
        return new BotDbContext(optionsBuilder.Options);
    }
}
