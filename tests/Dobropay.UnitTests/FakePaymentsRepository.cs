using Dobropay.Payments.Domain;
using Dobropay.Payments.Persistence;

namespace Dobropay.UnitTests;

public sealed class FakePaymentsRepository : IPaymentsRepository
{
    public List<Payment> Payments { get; } = [];
    public List<IdempotencyRecord> IdempotencyRecords { get; } = [];
    public List<CheckoutLink> CheckoutLinks { get; } = [];
    public List<PaymentAttempt> Attempts { get; } = [];
    public List<ProviderInbox> InboxItems { get; } = [];
    public List<OutboxEvent> OutboxEvents { get; } = [];
    public List<SavedPaymentMethod> SavedPaymentMethods { get; } = [];

    public Task<IdempotencyRecord?> FindIdempotencyRecordAsync(
        Guid shopId,
        string operation,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        Task.FromResult(IdempotencyRecords.SingleOrDefault(
            x => x.ShopId == shopId && x.Operation == operation && x.IdempotencyKey == idempotencyKey));

    public Task<Payment?> FindPaymentByIdAsync(Guid shopId, Guid paymentId, CancellationToken cancellationToken) =>
        Task.FromResult(Payments.SingleOrDefault(x => x.ShopId == shopId && x.Id == paymentId));

    public Task<Payment?> FindTrackedPaymentByIdAsync(Guid shopId, Guid paymentId, CancellationToken cancellationToken) =>
        Task.FromResult(Payments.SingleOrDefault(x => x.ShopId == shopId && x.Id == paymentId));

    public Task<IReadOnlyList<PaymentView>> ListShopPaymentsAsync(
        Guid shopId,
        int limit,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PaymentView>>(Payments
            .Where(x => x.ShopId == shopId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .Select(x => new PaymentView(
                x.Id,
                x.Status,
                x.Amount,
                x.Currency,
                x.ExternalOrderId,
                x.Capture,
                x.CapturedAmount,
                x.RefundedAmount,
                x.CancellationParty,
                x.CancellationReason))
            .ToList());

    public Task<CheckoutLink?> FindCheckoutLinkAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(CheckoutLinks.SingleOrDefault(x => x.TokenHash == tokenHash));

    public Task<PaymentAttempt?> FindAttemptByPaymentAsync(
        Guid shopId,
        Guid paymentId,
        CancellationToken cancellationToken) =>
        Task.FromResult(Attempts.SingleOrDefault(x => x.ShopId == shopId && x.PaymentId == paymentId));

    public Task<PaymentAttempt?> FindTrackedAttemptByPaymentAsync(
        Guid shopId,
        Guid paymentId,
        CancellationToken cancellationToken) =>
        Task.FromResult(Attempts.SingleOrDefault(x => x.ShopId == shopId && x.PaymentId == paymentId));

    public Task<PaymentAttempt?> FindAttemptByIdAsync(Guid attemptId, CancellationToken cancellationToken) =>
        Task.FromResult(Attempts.SingleOrDefault(x => x.Id == attemptId));

    public Task<PaymentAttempt?> FindTrackedAttemptByIdAsync(
        Guid shopId,
        Guid attemptId,
        CancellationToken cancellationToken) =>
        Task.FromResult(Attempts.SingleOrDefault(x => x.ShopId == shopId && x.Id == attemptId));

    public Task AddPaymentAsync(
        Payment payment,
        IdempotencyRecord idempotencyRecord,
        CheckoutLink checkoutLink,
        CancellationToken cancellationToken)
    {
        Payments.Add(payment);
        IdempotencyRecords.Add(idempotencyRecord);
        CheckoutLinks.Add(checkoutLink);
        return Task.CompletedTask;
    }

    public Task AddIdempotencyRecordAsync(IdempotencyRecord record, CancellationToken cancellationToken)
    {
        IdempotencyRecords.Add(record);
        return Task.CompletedTask;
    }

    public Task AddAttemptAsync(PaymentAttempt attempt, CancellationToken cancellationToken)
    {
        Attempts.Add(attempt);
        return Task.CompletedTask;
    }

    public Task SetAttemptExternalIdAsync(Guid attemptId, string externalId, CancellationToken cancellationToken)
    {
        var attempt = Attempts.SingleOrDefault(x => x.Id == attemptId && x.ExternalId is null);
        if (attempt is not null)
        {
            attempt.ExternalId = externalId;
        }

        return Task.CompletedTask;
    }

    public Task AddProviderEventAndOutboxAsync(
        ProviderInbox inbox,
        OutboxEvent outboxEvent,
        CancellationToken cancellationToken)
    {
        InboxItems.Add(inbox);
        OutboxEvents.Add(outboxEvent);
        return Task.CompletedTask;
    }

    public Task<OutboxEvent?> FindNextUnpublishedOutboxAsync(CancellationToken cancellationToken) =>
        Task.FromResult(OutboxEvents.Where(x => !x.Published).OrderBy(x => x.OccurredAt).FirstOrDefault());

    public Task<ProviderInbox?> FindTrackedInboxAsync(
        Guid inboxId,
        Guid shopId,
        CancellationToken cancellationToken) =>
        Task.FromResult(InboxItems.SingleOrDefault(x => x.Id == inboxId && x.ShopId == shopId));

    public Task<IReadOnlyList<PaymentAttempt>> ListStaleUnknownAttemptsAsync(
        DateTimeOffset createdBefore,
        int limit,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PaymentAttempt>>(Attempts
            .Where(x => x.State == PaymentRules.AttemptUnknown && x.ExternalId is not null && x.CreatedAt <= createdBefore)
            .OrderBy(x => x.CreatedAt)
            .Take(limit)
            .ToList());

    public Task<IReadOnlyList<Payment>> ListExpiredActivePaymentsAsync(
        DateTimeOffset now,
        int limit,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Payment>>(Payments
            .Where(x => (x.Status == PaymentRules.Pending || x.Status == PaymentRules.WaitingForCapture) && x.ExpiresAt <= now)
            .OrderBy(x => x.ExpiresAt)
            .Take(limit)
            .ToList());

    public Task AddOutboxEventAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken)
    {
        OutboxEvents.Add(outboxEvent);
        return Task.CompletedTask;
    }

    public Task<SavedPaymentMethod?> FindSavedPaymentMethodAsync(
        Guid shopId,
        Guid methodId,
        CancellationToken cancellationToken) =>
        Task.FromResult(SavedPaymentMethods.SingleOrDefault(x => x.ShopId == shopId && x.Id == methodId));

    public Task<IReadOnlyList<SavedPaymentMethodView>> ListSavedPaymentMethodsAsync(
        Guid shopId,
        int limit,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SavedPaymentMethodView>>(SavedPaymentMethods
            .Where(x => x.ShopId == shopId)
            .OrderByDescending(x => x.SavedAt)
            .Take(limit)
            .Select(x => new SavedPaymentMethodView(
                x.Id,
                x.Type,
                x.CardFirst6,
                x.CardLast4,
                x.ExpiryMonth,
                x.ExpiryYear,
                x.CardBrand,
                x.SavedAt))
            .ToList());

    public Task AddSavedPaymentMethodAsync(
        SavedPaymentMethod method,
        CancellationToken cancellationToken)
    {
        SavedPaymentMethods.Add(method);
        return Task.CompletedTask;
    }

    public void ClearTracking()
    {
    }
}
