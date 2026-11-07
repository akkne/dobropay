using Dobropay.Contracts.Constants;

namespace Dobropay.Payments.Domain;

public sealed record PaymentView(
    Guid Id,
    string Status,
    long Amount,
    string Currency,
    string? ExternalOrderId,
    bool Capture = true,
    long? CapturedAmount = null,
    long RefundedAmount = 0,
    string? CancellationParty = null,
    string? CancellationReason = null,
    string PaymentMethodType = PaymentMethodTypes.Sandbox,
    string ConfirmationType = ConfirmationTypes.Redirect,
    string? ConfirmationData = null,
    bool SavePaymentMethod = false,
    Guid? SavedPaymentMethodId = null);
