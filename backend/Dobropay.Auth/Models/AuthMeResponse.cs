using System.Text.Json.Serialization;

namespace Dobropay.Auth.Models;

public sealed record AuthMeResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("email")] string Email);
