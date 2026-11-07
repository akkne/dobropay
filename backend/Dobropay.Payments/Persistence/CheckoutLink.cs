namespace Dobropay.Payments.Persistence;

public sealed class CheckoutLink
{
    public string TokenHash { get; set; } = string.Empty;
    public Guid ShopId { get; set; }
    public Guid PaymentId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
