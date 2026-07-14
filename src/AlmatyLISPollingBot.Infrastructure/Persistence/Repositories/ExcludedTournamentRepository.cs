using System.Data;
using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AlmatyLISPollingBot.Infrastructure.Persistence.Repositories;

public sealed class ExcludedTournamentRepository : IExcludedTournamentRepository
{
    private readonly BotDbContext dbContext;

    public ExcludedTournamentRepository(BotDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<int>> AddMissingAsync(
        IReadOnlyCollection<int> tournamentIds,
        CancellationToken cancellationToken)
    {
        if (tournamentIds.Count == 0)
        {
            return Array.Empty<int>();
        }

        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = dbContext.Database.GetDbConnection().CreateCommand();
            var valuePlaceholders = new List<string>(tournamentIds.Count);
            var index = 0;
            foreach (var tournamentId in tournamentIds)
            {
                var idParameter = command.CreateParameter();
                idParameter.ParameterName = $"@id{index}";
                idParameter.DbType = DbType.Guid;
                idParameter.Value = Guid.NewGuid();
                command.Parameters.Add(idParameter);

                var tournamentIdParameter = command.CreateParameter();
                tournamentIdParameter.ParameterName = $"@tournamentId{index}";
                tournamentIdParameter.DbType = DbType.Int32;
                tournamentIdParameter.Value = tournamentId;
                command.Parameters.Add(tournamentIdParameter);

                valuePlaceholders.Add($"(@id{index}, @tournamentId{index})");
                index++;
            }

            command.CommandText = string.Concat(
                "INSERT INTO excluded_tournaments (\"Id\", \"TournamentId\") ",
                "VALUES ",
                string.Join(", ", valuePlaceholders),
                " ON CONFLICT (\"TournamentId\") DO NOTHING ",
                "RETURNING \"TournamentId\";");

            var addedTournamentIds = new List<int>(tournamentIds.Count);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                addedTournamentIds.Add(reader.GetInt32(0));
            }

            return addedTournamentIds;
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }
}
