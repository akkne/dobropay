namespace Dobropay.Payments.Domain;

public sealed record CreatePaymentRequest(
    long Amount,
    string Currency,
    string? ExternalOrderId,
    bool Capture = true,
    string? PaymentMethodType = null,
    string? ConfirmationType = null,
    bool SavePaymentMethod = false,
    Guid? PaymentMethodId = null,
    PaymentCardData? Card = null,
    string? CryptoNetwork = null);
