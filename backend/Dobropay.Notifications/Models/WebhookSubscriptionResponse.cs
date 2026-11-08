using System.Text.Json.Serialization;

namespace Dobropay.Notifications.Models;

public sealed record WebhookSubscriptionResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("shop_id")] Guid ShopId,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("signing_secret")] string SigningSecret,
    [property: JsonPropertyName("event_types")] IReadOnlyList<string>? EventTypes);
