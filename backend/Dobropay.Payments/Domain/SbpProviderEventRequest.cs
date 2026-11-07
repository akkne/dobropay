namespace Dobropay.Payments.Domain;

public sealed record SbpProviderEventRequest(
    string EventId,
    Guid ShopId,
    Guid AttemptId,
    string ExternalAttemptId,
    long Amount,
    string Currency,
    string Outcome,
    string SbpOperationId,
    string PayerBankId);
