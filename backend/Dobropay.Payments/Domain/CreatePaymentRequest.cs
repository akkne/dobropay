namespace Dobropay.Payments.Domain;

public sealed record CreatePaymentRequest(
    long Amount,
    string Currency,
    string? ExternalOrderId,
    bool Capture = true);
