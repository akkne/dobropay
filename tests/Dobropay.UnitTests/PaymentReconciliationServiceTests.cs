using Dobropay.Contracts.Constants;
using Dobropay.Payments.Domain;
using Dobropay.Payments.Persistence;
using Dobropay.Payments.Provider;
using Dobropay.Payments.Services;

namespace Dobropay.UnitTests;

public sealed class PaymentReconciliationServiceTests
{
    [Fact]
    public async Task CancelExpiredPaymentsAsync_CancelsExpiredPendingAndWaitingForCapturePayments()
    {
        var repository = new FakePaymentsRepository();
        var provider = new FakePaymentProvider();
        var router = new PaymentProviderRouter([provider]);
        var unitOfWork = new FakeUnitOfWork();
        var options = new ReconciliationOptions
        {
            IntervalSeconds = 10,
            AttemptStaleSeconds = 15,
            PaymentExpirationMinutes = 30,
            CaptureExpirationMinutes = 30
        };
        var service = new PaymentReconciliationService(repository, router, unitOfWork, options);

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
    public async Task ReconcileStaleAttemptsAsync_ResolvesStaleUnknownAttemptsAcrossPaymentMethods()
    {
        var repository = new FakePaymentsRepository();
        var sandboxProvider = new FakePaymentProvider(PaymentMethodTypes.Sandbox);
        var sbpProvider = new FakePaymentProvider(PaymentMethodTypes.Sbp);
        var router = new PaymentProviderRouter([sandboxProvider, sbpProvider]);
        var unitOfWork = new FakeUnitOfWork();
        var options = new ReconciliationOptions
        {
            IntervalSeconds = 10,
            AttemptStaleSeconds = 15,
            PaymentExpirationMinutes = 30,
            CaptureExpirationMinutes = 30
        };
        var service = new PaymentReconciliationService(repository, router, unitOfWork, options);

        var now = DateTimeOffset.UtcNow;
        var shopId = Guid.NewGuid();
        var payment1 = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 45000,
            Status = PaymentRules.Pending,
            Capture = false,
            PaymentMethodType = PaymentMethodTypes.Sandbox,
            ExpiresAt = now.AddMinutes(20)
        };
        var attempt1 = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            PaymentId = payment1.Id,
            OperationKey = Guid.NewGuid(),
            ExternalId = "sim-stale-1",
            PaymentMethodType = PaymentMethodTypes.Sandbox,
            State = PaymentRules.AttemptUnknown,
            CreatedAt = now.AddSeconds(-30)
        };

        var payment2 = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 80000,
            Status = PaymentRules.Pending,
            Capture = true,
            PaymentMethodType = PaymentMethodTypes.Sbp,
            ExpiresAt = now.AddMinutes(20)
        };
        var attempt2 = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            PaymentId = payment2.Id,
            OperationKey = Guid.NewGuid(),
            ExternalId = "sbp-stale-1",
            PaymentMethodType = PaymentMethodTypes.Sbp,
            State = PaymentRules.AttemptUnknown,
            CreatedAt = now.AddSeconds(-40)
        };

        repository.Payments.AddRange([payment1, payment2]);
        repository.Attempts.AddRange([attempt1, attempt2]);
        sandboxProvider.SetAttemptState("sim-stale-1", PaymentRules.Succeeded);
        sbpProvider.SetAttemptState("sbp-stale-1", PaymentRules.Succeeded);

        var reconciledCount = await service.ReconcileStaleAttemptsAsync(now, CancellationToken.None);

        Assert.Equal(2, reconciledCount);
        Assert.Equal(PaymentRules.Succeeded, attempt1.State);
        Assert.Equal(PaymentRules.WaitingForCapture, payment1.Status);
        Assert.Equal(PaymentRules.Succeeded, attempt2.State);
        Assert.Equal(PaymentRules.Succeeded, payment2.Status);
        Assert.Equal(80000, payment2.CapturedAmount);
        Assert.Equal(2, repository.OutboxEvents.Count);
    }
}
