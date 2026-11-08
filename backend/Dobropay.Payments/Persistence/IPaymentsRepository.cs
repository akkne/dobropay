using Dobropay.Payments.Domain;

namespace Dobropay.Payments.Persistence;

public interface IPaymentsRepository
{
    Task<IdempotencyRecord?> FindIdempotencyRecordAsync(Guid shopId, string operation, string idempotencyKey, CancellationToken cancellationToken);
    Task<Payment?> FindPaymentByIdAsync(Guid shopId, Guid paymentId, CancellationToken cancellationToken);
    Task<Payment?> FindTrackedPaymentByIdAsync(Guid shopId, Guid paymentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PaymentView>> ListShopPaymentsAsync(Guid shopId, int limit, CancellationToken cancellationToken);
    Task<CheckoutLink?> FindCheckoutLinkAsync(string tokenHash, CancellationToken cancellationToken);
    Task<PaymentAttempt?> FindAttemptByPaymentAsync(Guid shopId, Guid paymentId, CancellationToken cancellationToken);
    Task<PaymentAttempt?> FindTrackedAttemptByPaymentAsync(Guid shopId, Guid paymentId, CancellationToken cancellationToken);
    Task<PaymentAttempt?> FindAttemptByIdAsync(Guid attemptId, CancellationToken cancellationToken);
    Task<PaymentAttempt?> FindTrackedAttemptByIdAsync(Guid shopId, Guid attemptId, CancellationToken cancellationToken);
    Task AddPaymentAsync(Payment payment, IdempotencyRecord idempotencyRecord, CheckoutLink? checkoutLink, CancellationToken cancellationToken);
    Task AddIdempotencyRecordAsync(IdempotencyRecord record, CancellationToken cancellationToken);
    Task AddAttemptAsync(PaymentAttempt attempt, CancellationToken cancellationToken);
    Task SetAttemptExternalIdAsync(Guid attemptId, string externalId, CancellationToken cancellationToken);
    Task AddProviderEventAndOutboxAsync(ProviderInbox inbox, OutboxEvent outboxEvent, CancellationToken cancellationToken);
    Task<OutboxEvent?> FindNextUnpublishedOutboxAsync(CancellationToken cancellationToken);
    Task<ProviderInbox?> FindTrackedInboxAsync(Guid inboxId, Guid shopId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PaymentAttempt>> ListStaleUnknownAttemptsAsync(DateTimeOffset createdBefore, int limit, CancellationToken cancellationToken);
    Task<IReadOnlyList<Payment>> ListExpiredActivePaymentsAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken);
    Task AddOutboxEventAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken);
    Task<SavedPaymentMethod?> FindSavedPaymentMethodAsync(Guid shopId, Guid methodId, CancellationToken cancellationToken);
    Task<SavedPaymentMethodView?> FindSavedPaymentMethodViewAsync(Guid shopId, Guid methodId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SavedPaymentMethodView>> ListSavedPaymentMethodsAsync(Guid shopId, int limit, CancellationToken cancellationToken);
    Task AddSavedPaymentMethodAsync(SavedPaymentMethod method, CancellationToken cancellationToken);
    Task AddReceiptPayloadAsync(PaymentReceiptPayload payload, CancellationToken cancellationToken);
    Task<PaymentReceiptPayload?> FindReceiptPayloadAsync(Guid shopId, Guid paymentId, Guid? refundId, CancellationToken cancellationToken);
    void ClearTracking();
}
