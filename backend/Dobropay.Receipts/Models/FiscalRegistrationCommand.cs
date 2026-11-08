using Dobropay.Contracts.Receipts;

namespace Dobropay.Receipts.Models;

public sealed record FiscalRegistrationCommand(
    Guid ReceiptId,
    Guid ShopId,
    Guid PaymentId,
    Guid? RefundId,
    string ReceiptType,
    long TotalAmount,
    string Currency,
    int TaxSystemCode,
    string? CustomerEmail,
    string? CustomerPhone,
    IReadOnlyList<ReceiptItemDto> Items);
