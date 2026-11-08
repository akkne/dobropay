using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.Messaging.Metrics;
using Dobropay.Payments.Domain;
using Dobropay.Payments.Persistence;
using Dobropay.Payments.Provider;
using Dobropay.Payments.Services;

namespace Dobropay.UnitTests.Payments;

public sealed class TwoStagePaymentLifecycleTests
{
    private readonly FakePaymentsRepository repository = new();
    private readonly FakeUnitOfWork unitOfWork = new();
    private readonly Sha256HashProvider hashProvider = new();
    private readonly OpenTelemetryMetricsRecorder metricsRecorder = new();

    [Fact]
    public async Task FullCapture_WhenWaitingForCapture_TransitionsToSucceededWithFullAmount()
    {
        var service = new PaymentCaptureService(repository, unitOfWork, hashProvider, metricsRecorder);
        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 100000,
            Currency = DomainConstants.DefaultCurrency,
            Status = PaymentRules.WaitingForCapture,
            Capture = false
        };
        repository.Payments.Add(payment);

        var result = await service.CaptureAsync(
            shopId,
            payment.Id,
            "full-cap-key",
            new CapturePaymentRequest(null),
            "corr-full",
            CancellationToken.None);

        Assert.Equal(PaymentOperationOutcome.Success, result.Outcome);
        Assert.NotNull(result.Payment);
        Assert.Equal(PaymentRules.Succeeded, result.Payment.Status);
        Assert.Equal(100000, result.Payment.CapturedAmount);
        Assert.NotNull(payment.CapturedAt);
        Assert.Single(repository.OutboxEvents);
        Assert.Equal(1, metricsRecorder.GetPaymentCount(PaymentRules.Succeeded, DomainConstants.DefaultCurrency));
    }

    [Fact]
    public async Task PartialCapture_WhenWaitingForCapture_TransitionsToSucceededWithPartialAmount()
    {
        var service = new PaymentCaptureService(repository, unitOfWork, hashProvider, metricsRecorder);
        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 100000,
            Currency = DomainConstants.DefaultCurrency,
            Status = PaymentRules.WaitingForCapture,
            Capture = false
        };
        repository.Payments.Add(payment);

        var result = await service.CaptureAsync(
            shopId,
            payment.Id,
            "part-cap-key",
            new CapturePaymentRequest(45000),
            "corr-part",
            CancellationToken.None);

        Assert.Equal(PaymentOperationOutcome.Success, result.Outcome);
        Assert.NotNull(result.Payment);
        Assert.Equal(PaymentRules.Succeeded, result.Payment.Status);
        Assert.Equal(45000, result.Payment.CapturedAmount);
        Assert.NotNull(payment.CapturedAt);
        Assert.Single(repository.OutboxEvents);
        Assert.Equal(1, metricsRecorder.GetPaymentCount(PaymentRules.Succeeded, DomainConstants.DefaultCurrency));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    [InlineData(100001)]
    public async Task Capture_WithInvalidAmount_ReturnsInvalidAmountAndDoesNotMutateState(long invalidAmount)
    {
        var service = new PaymentCaptureService(repository, unitOfWork, hashProvider, metricsRecorder);
        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 100000,
            Currency = DomainConstants.DefaultCurrency,
            Status = PaymentRules.WaitingForCapture,
            Capture = false
        };
        repository.Payments.Add(payment);

        var result = await service.CaptureAsync(
            shopId,
            payment.Id,
            "invalid-cap-key",
            new CapturePaymentRequest(invalidAmount),
            "corr-invalid",
            CancellationToken.None);

        Assert.Equal(PaymentOperationOutcome.InvalidAmount, result.Outcome);
        Assert.Equal(PaymentRules.WaitingForCapture, payment.Status);
        Assert.Null(payment.CapturedAmount);
        Assert.Empty(repository.OutboxEvents);
        Assert.Equal(0, metricsRecorder.GetPaymentCount(PaymentRules.Succeeded, DomainConstants.DefaultCurrency));
    }

    [Theory]
    [InlineData(PaymentRules.Pending)]
    [InlineData(PaymentRules.Succeeded)]
    [InlineData(PaymentRules.Canceled)]
    public async Task Capture_WhenNotInWaitingForCaptureState_ReturnsInvalidState(string initialStatus)
    {
        var service = new PaymentCaptureService(repository, unitOfWork, hashProvider, metricsRecorder);
        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 100000,
            Currency = DomainConstants.DefaultCurrency,
            Status = initialStatus,
            Capture = false
        };
        repository.Payments.Add(payment);

        var result = await service.CaptureAsync(
            shopId,
            payment.Id,
            "state-cap-key",
            new CapturePaymentRequest(50000),
            "corr-state",
            CancellationToken.None);

        Assert.Equal(PaymentOperationOutcome.InvalidState, result.Outcome);
        Assert.Equal(initialStatus, payment.Status);
        Assert.Empty(repository.OutboxEvents);
    }

    [Fact]
    public async Task Cancel_WhenWaitingForCapture_TransitionsToCanceledWithMerchantParty()
    {
        var service = new PaymentCaptureService(repository, unitOfWork, hashProvider, metricsRecorder);
        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 80000,
            Currency = DomainConstants.DefaultCurrency,
            Status = PaymentRules.WaitingForCapture,
            Capture = false
        };
        repository.Payments.Add(payment);

        var result = await service.CancelAsync(
            shopId,
            payment.Id,
            "cancel-hold-key",
            new CancelPaymentRequest("buyer_declined"),
            "corr-cancel",
            CancellationToken.None);

        Assert.Equal(PaymentOperationOutcome.Success, result.Outcome);
        Assert.NotNull(result.Payment);
        Assert.Equal(PaymentRules.Canceled, result.Payment.Status);
        Assert.Equal(PaymentCancellationParties.Merchant, result.Payment.CancellationParty);
        Assert.Equal("buyer_declined", result.Payment.CancellationReason);
        Assert.Single(repository.OutboxEvents);
        Assert.Equal(1, metricsRecorder.GetPaymentCount(PaymentRules.Canceled, DomainConstants.DefaultCurrency));
    }

    [Fact]
    public async Task CancelExpiredPaymentsAsync_WhenHoldExpired_TransitionsToCanceledWithExpiredOnCapture()
    {
        var router = new PaymentProviderRouter([new FakePaymentProvider()]);
        var options = new ReconciliationOptions
        {
            IntervalSeconds = 10,
            AttemptStaleSeconds = 15,
            PaymentExpirationMinutes = 30,
            CaptureExpirationMinutes = 30
        };
        var reconciliationService = new PaymentReconciliationService(repository, router, unitOfWork, options, metricsRecorder);

        var shopId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var expiredPayment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 60000,
            Currency = DomainConstants.DefaultCurrency,
            Status = PaymentRules.WaitingForCapture,
            ExpiresAt = now.AddMinutes(-5),
            Capture = false
        };
        repository.Payments.Add(expiredPayment);

        var canceledCount = await reconciliationService.CancelExpiredPaymentsAsync(now, CancellationToken.None);

        Assert.Equal(1, canceledCount);
        Assert.Equal(PaymentRules.Canceled, expiredPayment.Status);
        Assert.Equal(PaymentCancellationParties.Dobropay, expiredPayment.CancellationParty);
        Assert.Equal(PaymentCancellationReasons.ExpiredOnCapture, expiredPayment.CancellationReason);
        Assert.Single(repository.OutboxEvents);
        Assert.Equal(1, metricsRecorder.GetPaymentCount(PaymentRules.Canceled, DomainConstants.DefaultCurrency));
    }
}
