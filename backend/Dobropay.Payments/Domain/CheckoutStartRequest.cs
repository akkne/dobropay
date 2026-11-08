namespace Dobropay.Payments.Domain;

public sealed record CheckoutStartRequest(
    string? Method = null);
