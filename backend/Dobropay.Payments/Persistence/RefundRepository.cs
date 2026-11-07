using Dobropay.Payments.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dobropay.Payments.Persistence;

public sealed class RefundRepository(PaymentsDbContext dbContext) : IRefundRepository
{
    public Task<Refund?> FindByIdAsync(Guid shopId, Guid refundId, CancellationToken cancellationToken) =>
        dbContext.Refunds
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ShopId == shopId && x.Id == refundId, cancellationToken);

    public async Task<IReadOnlyList<RefundView>> ListByPaymentAsync(
        Guid shopId,
        Guid paymentId,
        CancellationToken cancellationToken) =>
        await dbContext.Refunds
            .AsNoTracking()
            .Where(x => x.ShopId == shopId && x.PaymentId == paymentId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new RefundView(
                x.Id,
                x.PaymentId,
                x.Status,
                x.Amount,
                x.Currency,
                x.Reason,
                x.ExternalRefundId,
                x.CreatedAt,
                x.CompletedAt))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RefundView>> ListByShopAsync(
        Guid shopId,
        int limit,
        CancellationToken cancellationToken) =>
        await dbContext.Refunds
            .AsNoTracking()
            .Where(x => x.ShopId == shopId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .Select(x => new RefundView(
                x.Id,
                x.PaymentId,
                x.Status,
                x.Amount,
                x.Currency,
                x.Reason,
                x.ExternalRefundId,
                x.CreatedAt,
                x.CompletedAt))
            .ToListAsync(cancellationToken);

    public async Task AddRefundAsync(Refund refund, CancellationToken cancellationToken) =>
        await dbContext.Refunds.AddAsync(refund, cancellationToken);
}
