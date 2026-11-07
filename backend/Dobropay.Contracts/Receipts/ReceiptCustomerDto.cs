namespace Dobropay.Contracts.Receipts;

public sealed record ReceiptCustomerDto(
    string? Email = null,
    string? Phone = null);
