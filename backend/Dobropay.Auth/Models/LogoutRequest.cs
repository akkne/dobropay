using System.Text.Json.Serialization;

namespace Dobropay.Auth.Models;

public sealed record LogoutRequest(
    [property: JsonPropertyName("token")] string? Token = null,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken = null);
