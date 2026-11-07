using Dobropay.Payments.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dobropay.Payments.Persistence;

public sealed class PaymentsRepository(PaymentsDbContext dbContext) : IPaymentsRepository
{
    public Task<IdempotencyRecord?> FindIdempotencyRecordAsync(
        Guid shopId,
        string operation,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        dbContext.IdempotencyRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.ShopId == shopId && x.Operation == operation && x.IdempotencyKey == idempotencyKey,
                cancellationToken);

    public Task<Payment?> FindPaymentByIdAsync(Guid shopId, Guid paymentId, CancellationToken cancellationToken) =>
        dbContext.Payments
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ShopId == shopId && x.Id == paymentId, cancellationToken);

    public Task<Payment?> FindTrackedPaymentByIdAsync(Guid shopId, Guid paymentId, CancellationToken cancellationToken) =>
        dbContext.Payments
            .SingleOrDefaultAsync(x => x.ShopId == shopId && x.Id == paymentId, cancellationToken);

    public async Task<IReadOnlyList<PaymentView>> ListShopPaymentsAsync(
        Guid shopId,
        int limit,
        CancellationToken cancellationToken) =>
        await dbContext.Payments
            .AsNoTracking()
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
            .ToListAsync(cancellationToken);

    public Task<CheckoutLink?> FindCheckoutLinkAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.CheckoutLinks
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

    public Task<PaymentAttempt?> FindAttemptByPaymentAsync(
        Guid shopId,
        Guid paymentId,
        CancellationToken cancellationToken) =>
        dbContext.PaymentAttempts
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ShopId == shopId && x.PaymentId == paymentId, cancellationToken);

    public Task<PaymentAttempt?> FindTrackedAttemptByPaymentAsync(
        Guid shopId,
        Guid paymentId,
        CancellationToken cancellationToken) =>
        dbContext.PaymentAttempts
            .SingleOrDefaultAsync(x => x.ShopId == shopId && x.PaymentId == paymentId, cancellationToken);

    public Task<PaymentAttempt?> FindAttemptByIdAsync(Guid attemptId, CancellationToken cancellationToken) =>
        dbContext.PaymentAttempts
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == attemptId, cancellationToken);

    public Task<PaymentAttempt?> FindTrackedAttemptByIdAsync(
        Guid shopId,
        Guid attemptId,
        CancellationToken cancellationToken) =>
        dbContext.PaymentAttempts
            .SingleOrDefaultAsync(x => x.ShopId == shopId && x.Id == attemptId, cancellationToken);

    public async Task AddPaymentAsync(
        Payment payment,
        IdempotencyRecord idempotencyRecord,
        CheckoutLink checkoutLink,
        CancellationToken cancellationToken)
    {
        await dbContext.Payments.AddAsync(payment, cancellationToken);
        await dbContext.IdempotencyRecords.AddAsync(idempotencyRecord, cancellationToken);
        await dbContext.CheckoutLinks.AddAsync(checkoutLink, cancellationToken);
    }

    public async Task AddIdempotencyRecordAsync(
        IdempotencyRecord record,
        CancellationToken cancellationToken) =>
        await dbContext.IdempotencyRecords.AddAsync(record, cancellationToken);

    public async Task AddAttemptAsync(PaymentAttempt attempt, CancellationToken cancellationToken) =>
        await dbContext.PaymentAttempts.AddAsync(attempt, cancellationToken);

    public async Task SetAttemptExternalIdAsync(
        Guid attemptId,
        string externalId,
        CancellationToken cancellationToken) =>
        await dbContext.PaymentAttempts
            .Where(x => x.Id == attemptId && x.ExternalId == null)
            .ExecuteUpdateAsync(x => x.SetProperty(a => a.ExternalId, externalId), cancellationToken);

    public async Task AddProviderEventAndOutboxAsync(
        ProviderInbox inbox,
        OutboxEvent outboxEvent,
        CancellationToken cancellationToken)
    {
        await dbContext.ProviderInbox.AddAsync(inbox, cancellationToken);
        await dbContext.OutboxEvents.AddAsync(outboxEvent, cancellationToken);
    }

    public Task<OutboxEvent?> FindNextUnpublishedOutboxAsync(CancellationToken cancellationToken) =>
        dbContext.OutboxEvents
            .Where(x => !x.Published)
            .OrderBy(x => x.OccurredAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<ProviderInbox?> FindTrackedInboxAsync(
        Guid inboxId,
        Guid shopId,
        CancellationToken cancellationToken) =>
        dbContext.ProviderInbox
            .SingleOrDefaultAsync(x => x.Id == inboxId && x.ShopId == shopId, cancellationToken);

    public async Task AddOutboxEventAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken) =>
        await dbContext.OutboxEvents.AddAsync(outboxEvent, cancellationToken);

    public void ClearTracking() => dbContext.ChangeTracker.Clear();
}
