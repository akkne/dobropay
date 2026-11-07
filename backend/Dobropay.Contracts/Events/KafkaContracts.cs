namespace Dobropay.Contracts.Events;

public static class KafkaContracts
{
    public const string ProviderEvents = "dobropay.provider-events.v1";
    public const string PaymentEvents = "dobropay.payment-events.v1";
    public const string PaymentsProviderGroup = "payments-provider-events-v1";
    public const string NotificationsPaymentGroup = "notifications-payment-events-v1";
    public const string ReceiptsPaymentGroup = "receipts-payment-events-v1";
    public const string PaymentsProviderConsumerName = "payments-provider-events";
    public const string NotificationsPaymentConsumerName = "notifications-payment-events";
    public const string ReceiptsPaymentConsumerName = "receipts-payment-events";
    public const string ProviderEventAcceptedType = "provider.event.accepted";
    public const string PaymentSucceededType = "payment.succeeded";
    public const string PaymentCanceledType = "payment.canceled";
    public const string PaymentEventPrefix = "payment.";

    public static string DeadLetterQueue(string consumer) => $"dobropay.dlq.{consumer}.v1";
}
