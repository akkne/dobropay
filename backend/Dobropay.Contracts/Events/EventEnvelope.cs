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

public static class KafkaContracts
{
    public const string ProviderEvents = "dobropay.provider-events.v1";
    public const string PaymentEvents = "dobropay.payment-events.v1";
    public const string PaymentsProviderGroup = "payments-provider-events-v1";
    public const string NotificationsPaymentGroup = "notifications-payment-events-v1";
    public const string ReceiptsPaymentGroup = "receipts-payment-events-v1";
    public static string DeadLetterQueue(string consumer) => $"dobropay.dlq.{consumer}.v1";
}
