namespace Dobropay.Payments.Domain;

public sealed record PaymentCardData(
    string CardFirst6,
    string CardLast4,
    int ExpiryMonth,
    int ExpiryYear,
    string? CardBrand = null);
