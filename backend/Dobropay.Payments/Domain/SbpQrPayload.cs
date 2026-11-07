namespace Dobropay.Payments.Domain;

public sealed record SbpQrPayload(
    string QrcId,
    string PayloadUrl,
    DateTimeOffset ExpiresAt);
