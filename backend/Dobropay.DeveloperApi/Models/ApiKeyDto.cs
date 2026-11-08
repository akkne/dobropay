using System.Text.Json.Serialization;

namespace Dobropay.DeveloperApi.Models;

public sealed record ApiKeyDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("mode")] string Mode,
    [property: JsonPropertyName("key_prefix")] string KeyPrefix,
    [property: JsonPropertyName("key_mask")] string KeyMask,
    [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt);
