using System.Text.Json.Serialization;

namespace Dobropay.DeveloperApi.Models;

public sealed record ShopListItemResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("verification_status")] string VerificationStatus);
