using Dobropay.Contracts.Constants;

namespace Dobropay.Payments.Persistence;

public sealed class PaymentReceiptPayload
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid? RefundId { get; set; }
    public string ReceiptType { get; set; } = ReceiptTypes.Payment;
    public int TaxSystemCode { get; set; } = DomainConstants.DefaultTaxSystemCode;
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string ItemsJson { get; set; } = DomainConstants.EmptyJsonArray;
    public DateTimeOffset CreatedAt { get; set; }
}
