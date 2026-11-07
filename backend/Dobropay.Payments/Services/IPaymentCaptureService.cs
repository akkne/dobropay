using Dobropay.Payments.Domain;

namespace Dobropay.Payments.Services;

public interface IPaymentCaptureService
{
    Task<PaymentOperationResult> CaptureAsync(
        Guid shopId,
        Guid paymentId,
        string idempotencyKey,
        CapturePaymentRequest request,
        string correlationId,
        CancellationToken cancellationToken);

    Task<PaymentOperationResult> CancelAsync(
        Guid shopId,
        Guid paymentId,
        string idempotencyKey,
        CancelPaymentRequest request,
        string correlationId,
        CancellationToken cancellationToken);
}
