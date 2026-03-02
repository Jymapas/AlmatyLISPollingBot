using System.Net.Http.Json;
using AlmatyLISPollingBot.Application.Abstractions.Tournaments;
using AlmatyLISPollingBot.Application.Contracts.Tournaments;

namespace AlmatyLISPollingBot.Infrastructure.Services;

public sealed class ChgkTournamentClient : IChgkTournamentClient
{
    private readonly HttpClient httpClient;

    public ChgkTournamentClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<TournamentSummary>> GetTournamentsByDateAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken)
    {
        var route = $"api/tournaments?dateStart[after]={targetDate:yyyy-MM-dd}&dateStart[before]={targetDate:yyyy-MM-dd}";
        return await GetCollectionAsync(route, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TournamentSummary>> GetTournamentsByIdsAsync(
        IReadOnlyCollection<int> tournamentIds,
        CancellationToken cancellationToken)
    {
        var result = new List<TournamentSummary>(tournamentIds.Count);
        foreach (var tournamentId in tournamentIds)
        {
            var route = $"api/tournaments/{tournamentId}";
            var payload = await httpClient.GetFromJsonAsync<TournamentDto>(route, cancellationToken);
            if (payload is not null)
            {
                result.Add(payload.ToSummary());
            }
        }

        return result;
    }

    private async Task<IReadOnlyCollection<TournamentSummary>> GetCollectionAsync(string route, CancellationToken cancellationToken)
    {
        var payload = await httpClient.GetFromJsonAsync<List<TournamentDto>>(route, cancellationToken);
        return payload?.Select(x => x.ToSummary()).ToArray() ?? Array.Empty<TournamentSummary>();
    }

    private sealed class TournamentDto
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public int Type { get; init; }
        public bool? GgRating { get; init; }
        public DateTimeOffset DateStart { get; init; }
        public DateTimeOffset DateEnd { get; init; }
        public decimal? DifficultyForecast { get; init; }

        public TournamentSummary ToSummary()
        {
            return new TournamentSummary(
                Id,
                Name,
                Type,
                GgRating ?? false,
                DateStart,
                DateEnd,
                DifficultyForecast,
                HasRussianLanguage: true);
        }
    }
}
