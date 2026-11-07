namespace Dobropay.Payments.Domain;

public sealed record CapturePaymentRequest(long? Amount = null);
