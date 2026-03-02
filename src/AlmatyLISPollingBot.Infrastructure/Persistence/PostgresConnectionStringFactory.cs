using AlmatyLISPollingBot.Application.Contracts.Bot;
using Npgsql;

namespace AlmatyLISPollingBot.Infrastructure.Persistence;

public static class PostgresConnectionStringFactory
{
    public static string Build(DatabaseConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = configuration.Host,
            Port = configuration.Port,
            Database = configuration.Name,
            Username = configuration.Username,
            Password = configuration.Password
        };

        return builder.ConnectionString;
    }
}
