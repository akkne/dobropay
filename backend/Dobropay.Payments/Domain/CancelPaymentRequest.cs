namespace Dobropay.Payments.Domain;

public sealed record CancelPaymentRequest(string? Reason = null);
