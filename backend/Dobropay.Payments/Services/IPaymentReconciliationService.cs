namespace Dobropay.Payments.Services;

public interface IPaymentReconciliationService
{
    Task<int> ReconcileStaleAttemptsAsync(DateTimeOffset now, CancellationToken cancellationToken);
    Task<int> CancelExpiredPaymentsAsync(DateTimeOffset now, CancellationToken cancellationToken);
}
