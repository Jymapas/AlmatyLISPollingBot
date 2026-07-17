using System.Data;
using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlmatyLISPollingBot.Infrastructure.Persistence.Repositories;

public sealed class ForcedTournamentRepository : IForcedTournamentRepository
{
    private readonly BotDbContext dbContext;

    public ForcedTournamentRepository(BotDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<ForcedTournament>> GetQueuedAsync(CancellationToken cancellationToken)
    {
        return await dbContext.ForcedTournaments
            .AsNoTracking()
            .OrderBy(x => x.QueuedAtUtc)
            .ThenBy(x => x.TournamentId)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<int>> AddMissingAsync(
        IReadOnlyCollection<int> tournamentIds,
        DateTimeOffset queuedAtUtc,
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

                var queuedAtParameter = command.CreateParameter();
                queuedAtParameter.ParameterName = $"@queuedAtUtc{index}";
                queuedAtParameter.DbType = DbType.DateTimeOffset;
                queuedAtParameter.Value = queuedAtUtc;
                command.Parameters.Add(queuedAtParameter);

                valuePlaceholders.Add($"(@id{index}, @tournamentId{index}, @queuedAtUtc{index})");
                index++;
            }

            command.CommandText = string.Concat(
                "INSERT INTO forced_tournaments (\"Id\", \"TournamentId\", \"QueuedAtUtc\") ",
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

    public async Task RemoveAsync(IReadOnlyCollection<int> tournamentIds, CancellationToken cancellationToken)
    {
        if (tournamentIds.Count == 0)
        {
            return;
        }

        var entities = await dbContext.ForcedTournaments
            .Where(x => tournamentIds.Contains(x.TournamentId))
            .ToArrayAsync(cancellationToken);
        dbContext.ForcedTournaments.RemoveRange(entities);
    }
}
