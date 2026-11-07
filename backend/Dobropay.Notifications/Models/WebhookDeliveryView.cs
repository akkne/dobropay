using System.Text.Json.Serialization;

namespace Dobropay.Notifications.Models;

public sealed record WebhookDeliveryView(
    [property: JsonPropertyName("event_id")] Guid EventId,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("attempts")] int Attempts,
    [property: JsonPropertyName("last_result")] string? LastResult);
