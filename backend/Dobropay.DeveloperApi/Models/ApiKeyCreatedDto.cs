using System.Text.Json.Serialization;

namespace Dobropay.DeveloperApi.Models;

public sealed record ApiKeyCreatedDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("mode")] string Mode,
    [property: JsonPropertyName("api_key")] string ApiKey,
    [property: JsonPropertyName("key_prefix")] string KeyPrefix,
    [property: JsonPropertyName("key_mask")] string KeyMask,
    [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt);
