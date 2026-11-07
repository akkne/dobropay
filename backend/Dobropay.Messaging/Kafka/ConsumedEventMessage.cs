namespace Dobropay.Messaging.Kafka;

public sealed record ConsumedEventMessage(string RawPayload, object NativeReceipt);
