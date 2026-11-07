using Dobropay.Contracts.Constants;

namespace Dobropay.Payments.Domain;

public sealed record CreateRefundRequest(
    Guid PaymentId,
    long Amount,
    string Currency = DomainConstants.DefaultCurrency,
    string? Reason = null);
