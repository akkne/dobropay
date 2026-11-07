namespace Dobropay.Payments.Domain;

public sealed record RefundProviderRequest(
    Guid RefundId,
    Guid PaymentId,
    long Amount,
    string Currency);
