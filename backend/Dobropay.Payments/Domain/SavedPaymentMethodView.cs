namespace Dobropay.Payments.Domain;

public sealed record SavedPaymentMethodView(
    Guid Id,
    string Type,
    string CardFirst6,
    string CardLast4,
    int ExpiryMonth,
    int ExpiryYear,
    string CardBrand,
    DateTimeOffset SavedAt);
