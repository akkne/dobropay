namespace Dobropay.Receipts.Persistence;

public sealed class ReceiptInbox
{
    public Guid EventId { get; set; }
    public Guid ShopId { get; set; }
    public Guid PaymentId { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
}
