using System.Text.Json.Serialization;

namespace Dobropay.Auth.Models;

public sealed record RefreshRequest(
    [property: JsonPropertyName("refresh_token")] string RefreshToken);
