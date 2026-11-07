using Dobropay.Contracts.Constants;

namespace Dobropay.Payments.Domain;

public sealed record CreatePaymentRefundRequest(
    long Amount,
    string Currency = DomainConstants.DefaultCurrency,
    string? Reason = null);
