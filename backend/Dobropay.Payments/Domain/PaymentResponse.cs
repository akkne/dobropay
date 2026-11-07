namespace Dobropay.Payments.Domain;

public sealed record PaymentResponse(Guid Id, string Status, long Amount, string Currency, string CheckoutUrl);
