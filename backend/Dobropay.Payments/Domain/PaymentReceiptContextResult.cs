using Dobropay.Contracts.Receipts;

namespace Dobropay.Payments.Domain;

public sealed record PaymentReceiptContextResult(
    PaymentReceiptContextOutcome Outcome,
    PaymentReceiptContextDto? Context = null,
    string? ErrorCode = null);
