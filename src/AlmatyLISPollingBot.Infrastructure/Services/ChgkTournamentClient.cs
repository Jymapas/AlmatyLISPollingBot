using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AlmatyLISPollingBot.Application.Abstractions.Tournaments;
using AlmatyLISPollingBot.Application.Contracts.Tournaments;
using AlmatyLISPollingBot.Domain.Common;
using AlmatyLISPollingBot.Infrastructure.Services.Chgk.Models;

namespace AlmatyLISPollingBot.Infrastructure.Services;

public sealed class ChgkTournamentClient : IChgkTournamentClient
{
    // The CHGK OpenAPI contract documents 512 as the maximum collection page size.
    private const int TournamentCollectionPageSize = 512;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient httpClient;

    public ChgkTournamentClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<TournamentDetails>> GetTournamentsIntersectingDateAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken)
    {
        var startOfDate = PollRules.GetSlotStart(targetDate, TimeOnly.MinValue);
        var startOfNextDate = startOfDate.AddDays(1);
        var route = string.Concat(
            "tournaments?dateStart%5Bbefore%5D=",
            Uri.EscapeDataString(startOfNextDate.ToString("O", CultureInfo.InvariantCulture)),
            "&dateEnd%5Bafter%5D=",
            Uri.EscapeDataString(startOfDate.ToString("O", CultureInfo.InvariantCulture)),
            "&itemsPerPage=",
            TournamentCollectionPageSize.ToString(CultureInfo.InvariantCulture));

        return await GetCollectionAsync(route, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TournamentDetails>> GetTournamentsByIdsAsync(
        IReadOnlyCollection<int> tournamentIds,
        CancellationToken cancellationToken)
    {
        var result = new List<TournamentDetails>(tournamentIds.Count);
        foreach (var tournamentId in tournamentIds)
        {
            var route = $"tournaments/{tournamentId}";
            var payload = await GetAsync<TournamentDto>(route, cancellationToken);
            if (payload is not null)
            {
                result.Add(payload.ToSummary());
            }
        }

        return result;
    }

    private async Task<IReadOnlyCollection<TournamentDetails>> GetCollectionAsync(string route, CancellationToken cancellationToken)
    {
        var result = new List<TournamentDetails>();
        var currentRoute = route;

        while (!string.IsNullOrWhiteSpace(currentRoute))
        {
            var payload = await GetAsync<TournamentCollectionDto>(currentRoute, cancellationToken);
            if (payload is null)
            {
                break;
            }

            result.AddRange(payload.Members.Select(x => x.ToSummary()));
            currentRoute = GetSafeRelativeRoute(payload.View?.Next);
        }

        return result;
    }

    private async Task<T?> GetAsync<T>(string route, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, route);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/ld+json"));
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"CHGK API returned HTTP {(int)response.StatusCode}.",
                null,
                response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<T>(SerializerOptions, cancellationToken);
    }

    private static string? GetSafeRelativeRoute(string? route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return null;
        }

        var trimmedRoute = route.Trim();
        if (!trimmedRoute.StartsWith("/", StringComparison.Ordinal)
            || trimmedRoute.StartsWith("//", StringComparison.Ordinal)
            || trimmedRoute.IndexOfAny(new[] { '\r', '\n', '\\' }) >= 0)
        {
            return null;
        }

        return trimmedRoute.TrimStart('/');
    }
}
