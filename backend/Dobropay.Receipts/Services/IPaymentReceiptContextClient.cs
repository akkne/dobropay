using Dobropay.Contracts.Receipts;

namespace Dobropay.Receipts.Services;

public interface IPaymentReceiptContextClient
{
    Task<PaymentReceiptContextDto?> GetReceiptContextAsync(
        Guid shopId,
        Guid paymentId,
        Guid? refundId,
        CancellationToken cancellationToken);
}
