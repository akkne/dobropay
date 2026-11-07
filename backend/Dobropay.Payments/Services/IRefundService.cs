using Dobropay.Payments.Domain;

namespace Dobropay.Payments.Services;

public interface IRefundService
{
    Task<RefundOperationResult> CreateRefundAsync(
        Guid shopId,
        string idempotencyKey,
        CreateRefundRequest request,
        string correlationId,
        CancellationToken cancellationToken);
}
