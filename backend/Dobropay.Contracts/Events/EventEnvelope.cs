using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dobropay.Contracts.Events;

public sealed record EventEnvelope(
    [property: JsonPropertyName("event_id")] Guid EventId,
    [property: JsonPropertyName("event_type")] string EventType,
    [property: JsonPropertyName("schema_version")] int SchemaVersion,
    [property: JsonPropertyName("occurred_at")] DateTimeOffset OccurredAt,
    [property: JsonPropertyName("shop_id")] Guid ShopId,
    [property: JsonPropertyName("payment_id")] Guid PaymentId,
    [property: JsonPropertyName("correlation_id")] string CorrelationId,
    [property: JsonPropertyName("payload")] JsonElement Payload);
