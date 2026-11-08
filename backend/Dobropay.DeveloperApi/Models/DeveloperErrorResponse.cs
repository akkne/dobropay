using System.Text.Json.Serialization;

namespace Dobropay.DeveloperApi.Models;

public sealed record DeveloperErrorResponse(
    [property: JsonPropertyName("error")] string Error,
    [property: JsonPropertyName("message")] string? Message = null);
