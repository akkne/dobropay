namespace Dobropay.Receipts.Persistence;

public sealed class FiscalReceipt
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid? RefundId { get; set; }
    public string ReceiptType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int TaxSystemCode { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public long? FiscalDocumentNumber { get; set; }
    public string? FiscalStorageNumber { get; set; }
    public string? FiscalAttribute { get; set; }
    public string? FiscalSign { get; set; }
    public string? QrPayload { get; set; }
    public DateTimeOffset? RegisteredAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<FiscalReceiptItem> Items { get; set; } = new List<FiscalReceiptItem>();
}
