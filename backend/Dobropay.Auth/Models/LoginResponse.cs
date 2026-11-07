using System.Text.Json.Serialization;

namespace Dobropay.Auth.Models;

public sealed record LoginResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("account_id")] Guid AccountId);
