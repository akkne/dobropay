using Dobropay.Contracts.Constants;
using Dobropay.Payments.Domain;
using Dobropay.Payments.Persistence;
using Dobropay.Payments.Services;

namespace Dobropay.UnitTests;

public sealed class PaymentReconciliationServiceTests
{
    [Fact]
    public async Task CancelExpiredPaymentsAsync_CancelsExpiredPendingAndWaitingForCapturePayments()
    {
        var repository = new FakePaymentsRepository();
        var provider = new FakePaymentProvider();
        var unitOfWork = new FakeUnitOfWork();
        var options = new ReconciliationOptions
        {
            IntervalSeconds = 10,
            AttemptStaleSeconds = 15,
            PaymentExpirationMinutes = 30,
            CaptureExpirationMinutes = 30
        };
        var service = new PaymentReconciliationService(repository, provider, unitOfWork, options);

        var now = DateTimeOffset.UtcNow;
        var shopId = Guid.NewGuid();
        var expiredPending = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 10000,
            Status = PaymentRules.Pending,
            ExpiresAt = now.AddMinutes(-1)
        };
        var expiredHold = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 20000,
            Status = PaymentRules.WaitingForCapture,
            Capture = false,
            ExpiresAt = now.AddSeconds(-5)
        };
        var activePending = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 30000,
            Status = PaymentRules.Pending,
            ExpiresAt = now.AddMinutes(10)
        };

        repository.Payments.AddRange([expiredPending, expiredHold, activePending]);

        var canceledCount = await service.CancelExpiredPaymentsAsync(now, CancellationToken.None);

        Assert.Equal(2, canceledCount);
        Assert.Equal(PaymentRules.Canceled, expiredPending.Status);
        Assert.Equal(PaymentCancellationParties.Dobropay, expiredPending.CancellationParty);
        Assert.Equal(PaymentCancellationReasons.ExpiredOnConfirmation, expiredPending.CancellationReason);

        Assert.Equal(PaymentRules.Canceled, expiredHold.Status);
        Assert.Equal(PaymentCancellationParties.Dobropay, expiredHold.CancellationParty);
        Assert.Equal(PaymentCancellationReasons.ExpiredOnCapture, expiredHold.CancellationReason);

        Assert.Equal(PaymentRules.Pending, activePending.Status);
        Assert.Equal(2, repository.OutboxEvents.Count);
    }

    [Fact]
    public async Task ReconcileStaleAttemptsAsync_ResolvesStaleUnknownAttemptWithProviderState()
    {
        var repository = new FakePaymentsRepository();
        var provider = new FakePaymentProvider();
        var unitOfWork = new FakeUnitOfWork();
        var options = new ReconciliationOptions
        {
            IntervalSeconds = 10,
            AttemptStaleSeconds = 15,
            PaymentExpirationMinutes = 30,
            CaptureExpirationMinutes = 30
        };
        var service = new PaymentReconciliationService(repository, provider, unitOfWork, options);

        var now = DateTimeOffset.UtcNow;
        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 45000,
            Status = PaymentRules.Pending,
            Capture = false,
            ExpiresAt = now.AddMinutes(20)
        };
        var attempt = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            PaymentId = payment.Id,
            OperationKey = Guid.NewGuid(),
            ExternalId = "sim-stale-1",
            State = PaymentRules.AttemptUnknown,
            CreatedAt = now.AddSeconds(-30)
        };

        repository.Payments.Add(payment);
        repository.Attempts.Add(attempt);
        provider.SetAttemptState("sim-stale-1", PaymentRules.Succeeded);

        var reconciledCount = await service.ReconcileStaleAttemptsAsync(now, CancellationToken.None);

        Assert.Equal(1, reconciledCount);
        Assert.Equal(PaymentRules.Succeeded, attempt.State);
        Assert.Equal(PaymentRules.WaitingForCapture, payment.Status);
        Assert.Single(repository.OutboxEvents);
    }
}
