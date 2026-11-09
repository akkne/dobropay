using System.Text.Json.Serialization;

namespace Dobropay.DeveloperApi.Models;

public sealed record CreateShopResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("onboarding")] ShopOnboardingDto Onboarding,
    [property: JsonPropertyName("default_key")] ApiKeyCreatedDto? DefaultKey);
