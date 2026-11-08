using System.Text.Json.Serialization;

namespace Dobropay.Auth.Models;

public sealed record AuthErrorResponse(
    [property: JsonPropertyName("error")] string Error);
