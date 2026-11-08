namespace Dobropay.Payments.Domain;

public sealed record CheckoutSessionDto(
    string Token,
    string PaymentId,
    string ShopId,
    string ShopName,
    string OrderId,
    decimal Amount,
    string Currency,
    string Status,
    string PaymentMethodType,
    bool Capture,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    string? Description = null,
    string? ConfirmationUrl = null);
