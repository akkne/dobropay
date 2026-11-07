namespace Dobropay.Payments.Domain;

public sealed record Card3dsCallbackRequest(
    string EventId,
    Guid ShopId,
    Guid AttemptId,
    string ExternalAttemptId,
    long Amount,
    string Currency,
    string Outcome,
    string? CardFirst6 = null,
    string? CardLast4 = null,
    int? ExpiryMonth = null,
    int? ExpiryYear = null,
    string? CardBrand = null);
