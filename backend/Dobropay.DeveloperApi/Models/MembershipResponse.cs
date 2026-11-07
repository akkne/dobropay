using System.Text.Json.Serialization;

namespace Dobropay.DeveloperApi.Models;

public sealed record MembershipResponse(
    [property: JsonPropertyName("shop_id")] Guid ShopId,
    [property: JsonPropertyName("role")] string Role);
