using Dobropay.Payments.Domain;

namespace Dobropay.Payments.Persistence;

public interface IRefundRepository
{
    Task<Refund?> FindByIdAsync(Guid shopId, Guid refundId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RefundView>> ListByPaymentAsync(Guid shopId, Guid paymentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RefundView>> ListByShopAsync(Guid shopId, int limit, CancellationToken cancellationToken);
    Task AddRefundAsync(Refund refund, CancellationToken cancellationToken);
}
