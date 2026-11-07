using Dobropay.Payments.Domain;
using Dobropay.Payments.Persistence;

namespace Dobropay.UnitTests;

public sealed class FakeRefundRepository : IRefundRepository
{
    public List<Refund> Refunds { get; } = [];

    public Task<Refund?> FindByIdAsync(Guid shopId, Guid refundId, CancellationToken cancellationToken) =>
        Task.FromResult(Refunds.SingleOrDefault(x => x.ShopId == shopId && x.Id == refundId));

    public Task<IReadOnlyList<RefundView>> ListByPaymentAsync(
        Guid shopId,
        Guid paymentId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RefundView>>(Refunds
            .Where(x => x.ShopId == shopId && x.PaymentId == paymentId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(MapView)
            .ToList());

    public Task<IReadOnlyList<RefundView>> ListByShopAsync(
        Guid shopId,
        int limit,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RefundView>>(Refunds
            .Where(x => x.ShopId == shopId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .Select(MapView)
            .ToList());

    public Task AddRefundAsync(Refund refund, CancellationToken cancellationToken)
    {
        Refunds.Add(refund);
        return Task.CompletedTask;
    }

    private static RefundView MapView(Refund refund) =>
        new(
            refund.Id,
            refund.PaymentId,
            refund.Status,
            refund.Amount,
            refund.Currency,
            refund.Reason,
            refund.ExternalRefundId,
            refund.CreatedAt,
            refund.CompletedAt);
}
