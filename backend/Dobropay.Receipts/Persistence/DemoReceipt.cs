using Dobropay.Contracts.Constants;

namespace Dobropay.Receipts.Persistence;

public sealed class DemoReceipt
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid EventId { get; set; }
    public long Amount { get; set; }
    public string Currency { get; set; } = DomainConstants.DefaultCurrency;
    public string EmailState { get; set; } = ReceiptsSchemaConstants.EmailStatePending;
    public DateTimeOffset CreatedAt { get; set; }
}
