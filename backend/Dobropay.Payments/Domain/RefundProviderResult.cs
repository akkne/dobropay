namespace Dobropay.Payments.Domain;

public sealed record RefundProviderResult(
    string ExternalRefundId,
    string Status);
