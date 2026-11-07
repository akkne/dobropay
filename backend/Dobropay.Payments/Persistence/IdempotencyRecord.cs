namespace Dobropay.Payments.Persistence;

public sealed class IdempotencyRecord
{
    public Guid ShopId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public Guid PaymentId { get; set; }
}
