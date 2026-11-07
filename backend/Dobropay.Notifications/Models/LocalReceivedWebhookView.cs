using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dobropay.Notifications.Models;

public sealed record LocalReceivedWebhookView(
    [property: JsonPropertyName("event_id")] Guid EventId,
    [property: JsonPropertyName("attempts")] int Attempts,
    [property: JsonPropertyName("verified")] bool Verified,
    [property: JsonPropertyName("body")] JsonElement Body);
