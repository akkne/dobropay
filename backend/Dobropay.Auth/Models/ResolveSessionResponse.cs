using System.Text.Json.Serialization;

namespace Dobropay.Auth.Models;

public sealed record ResolveSessionResponse(
    [property: JsonPropertyName("account_id")] Guid AccountId);
