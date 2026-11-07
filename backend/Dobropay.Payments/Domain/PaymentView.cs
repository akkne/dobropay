namespace Dobropay.Payments.Domain;

public sealed record PaymentView(Guid Id, string Status, long Amount, string Currency, string? ExternalOrderId);
