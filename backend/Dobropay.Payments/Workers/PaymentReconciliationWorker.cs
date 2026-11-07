using Dobropay.Payments.Services;

namespace Dobropay.Payments.Workers;

public sealed class PaymentReconciliationWorker(
    IServiceScopeFactory scopeFactory,
    ReconciliationOptions options,
    ILogger<PaymentReconciliationWorker> logger) : BackgroundService
{
    private const int MinIntervalSeconds = 1;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromSeconds(Math.Max(MinIntervalSeconds, options.IntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IPaymentReconciliationService>();
                var now = DateTimeOffset.UtcNow;

                var reconciled = await service.ReconcileStaleAttemptsAsync(now, stoppingToken);
                var expired = await service.CancelExpiredPaymentsAsync(now, stoppingToken);

                if (reconciled > 0 || expired > 0)
                {
                    logger.LogInformation(
                        "Payment reconciliation cycle completed reconciled {ReconciledCount} expired {ExpiredCount}",
                        reconciled,
                        expired);
                }
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Payment reconciliation cycle failed");
            }

            await Task.Delay(delay, stoppingToken);
        }
    }
}
