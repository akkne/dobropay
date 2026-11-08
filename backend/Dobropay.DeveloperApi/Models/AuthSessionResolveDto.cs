using System.Text.Json.Serialization;

namespace Dobropay.DeveloperApi.Models;

public sealed record AuthSessionResolveDto(
    [property: JsonPropertyName("account_id")] Guid AccountId);
