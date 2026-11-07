namespace Dobropay.Contracts.Receipts;

public sealed record PaymentReceiptContextDto(
    Guid ShopId,
    Guid PaymentId,
    Guid? RefundId,
    string ReceiptType,
    long Amount,
    string Currency,
    string? ExternalOrderId,
    string PaymentMethodType,
    int TaxSystemCode,
    string? CustomerEmail,
    string? CustomerPhone,
    IReadOnlyList<ReceiptItemDto> Items);
