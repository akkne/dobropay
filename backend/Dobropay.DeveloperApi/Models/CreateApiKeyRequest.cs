using System.Text.Json.Serialization;

namespace Dobropay.DeveloperApi.Models;

public sealed record CreateApiKeyRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("mode")] string Mode);
