namespace Dobropay.Receipts.Persistence;

public sealed class FiscalReceiptItem
{
    public Guid Id { get; set; }
    public Guid ReceiptId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;
    public long Amount { get; set; }
    public int VatCode { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public string PaymentSubject { get; set; } = string.Empty;

    public FiscalReceipt Receipt { get; set; } = null!;
}
