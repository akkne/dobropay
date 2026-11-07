namespace Dobropay.Payments.Domain;

public sealed record CryptoProviderEventRequest(
    string EventId,
    Guid ShopId,
    Guid AttemptId,
    string ExternalAttemptId,
    long Amount,
    string Currency,
    string Outcome,
    string TxHash,
    int Confirmations);
