namespace Dobropay.Receipts.Models;

public sealed record FiscalReceiptDto(
    Guid Id,
    Guid ShopId,
    Guid PaymentId,
    Guid? RefundId,
    string ReceiptType,
    string Status,
    long Amount,
    string Currency,
    int TaxSystemCode,
    string? CustomerEmail,
    string? CustomerPhone,
    long? FiscalDocumentNumber,
    string? FiscalStorageNumber,
    string? FiscalAttribute,
    string? FiscalSign,
    string? QrPayload,
    DateTimeOffset? RegisteredAt,
    DateTimeOffset CreatedAt,
    IReadOnlyList<FiscalReceiptItemDto> Items)
{
    public long? FdNumber => FiscalDocumentNumber;
    public string? FnNumber => FiscalStorageNumber;
    public string? FpdNumber => FiscalAttribute;
    public string FiscalStatus => Status;
    public string OrderNumber => PaymentId.ToString()[..8];
}
