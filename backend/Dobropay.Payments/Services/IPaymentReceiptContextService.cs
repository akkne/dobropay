using Dobropay.Payments.Domain;

namespace Dobropay.Payments.Services;

public interface IPaymentReceiptContextService
{
    Task<PaymentReceiptContextResult> GetReceiptContextAsync(
        Guid shopId,
        Guid paymentId,
        Guid? refundId,
        CancellationToken cancellationToken);
}
