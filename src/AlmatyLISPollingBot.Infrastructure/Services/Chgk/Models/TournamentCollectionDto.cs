using System.Text.Json.Serialization;

namespace AlmatyLISPollingBot.Infrastructure.Services.Chgk.Models;

internal sealed class TournamentCollectionDto
{
    [JsonPropertyName("member")]
    public IReadOnlyList<TournamentDto> Members { get; init; } = Array.Empty<TournamentDto>();

    public TournamentCollectionViewDto? View { get; init; }
}
