using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlmatyLISPollingBot.Infrastructure.Services.Chgk.Models;

internal sealed class TournamentLanguageCollectionJsonConverter : JsonConverter<IReadOnlyList<TournamentLanguageDto>>
{
    public override IReadOnlyList<TournamentLanguageDto> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.StartArray => JsonSerializer.Deserialize<List<TournamentLanguageDto>>(ref reader, options)?.ToArray()
                ?? Array.Empty<TournamentLanguageDto>(),
            JsonTokenType.StartObject => (JsonSerializer.Deserialize<Dictionary<string, TournamentLanguageDto>>(ref reader, options)
                ?? new Dictionary<string, TournamentLanguageDto>()).Values.ToArray(),
            JsonTokenType.Null => Array.Empty<TournamentLanguageDto>(),
            _ => throw new JsonException("CHGK API returned an unsupported languages value.")
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyList<TournamentLanguageDto> value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.ToArray(), options);
    }
}
