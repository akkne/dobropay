using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Receipts;

namespace Dobropay.Payments.Domain;

public sealed record CreateRefundRequest(
    Guid PaymentId,
    long Amount,
    string Currency = DomainConstants.DefaultCurrency,
    string? Reason = null,
    ReceiptRequestDto? Receipt = null);
