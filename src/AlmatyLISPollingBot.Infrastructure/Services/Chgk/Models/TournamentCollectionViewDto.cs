using System.Text.Json.Serialization;

namespace AlmatyLISPollingBot.Infrastructure.Services.Chgk.Models;

internal sealed class TournamentCollectionViewDto
{
    [JsonPropertyName("next")]
    public string? Next { get; init; }
}
