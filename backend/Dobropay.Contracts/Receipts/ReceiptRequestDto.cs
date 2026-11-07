using Dobropay.Contracts.Constants;

namespace Dobropay.Contracts.Receipts;

public sealed record ReceiptRequestDto(
    ReceiptCustomerDto? Customer,
    IReadOnlyList<ReceiptItemDto> Items,
    int? TaxSystemCode = DomainConstants.DefaultTaxSystemCode);
