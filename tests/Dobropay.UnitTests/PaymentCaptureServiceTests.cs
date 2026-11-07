using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.Payments.Domain;
using Dobropay.Payments.Persistence;
using Dobropay.Payments.Services;

namespace Dobropay.UnitTests;

public sealed class PaymentCaptureServiceTests
{
    [Fact]
    public async Task CaptureAsync_FullAndPartialCapture_TransitionsToSucceededAndPublishesOutbox()
    {
        var repository = new FakePaymentsRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = new PaymentCaptureService(repository, unitOfWork, new Sha256HashProvider());
        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 50000,
            Status = PaymentRules.WaitingForCapture,
            Capture = false
        };
        repository.Payments.Add(payment);

        var result = await service.CaptureAsync(
            shopId,
            payment.Id,
            "cap-key-1",
            new CapturePaymentRequest(35000),
            "corr-1",
            CancellationToken.None);

        Assert.Equal(PaymentOperationOutcome.Success, result.Outcome);
        Assert.NotNull(result.Payment);
        Assert.Equal(PaymentRules.Succeeded, result.Payment.Status);
        Assert.Equal(35000, result.Payment.CapturedAmount);
        Assert.Single(repository.OutboxEvents);
        Assert.Equal(1, unitOfWork.SaveCalls);

        var replay = await service.CaptureAsync(
            shopId,
            payment.Id,
            "cap-key-1",
            new CapturePaymentRequest(35000),
            "corr-2",
            CancellationToken.None);

        Assert.Equal(PaymentOperationOutcome.Success, replay.Outcome);
        Assert.Equal(35000, replay.Payment?.CapturedAmount);
        Assert.Single(repository.OutboxEvents);

        var conflict = await service.CaptureAsync(
            shopId,
            payment.Id,
            "cap-key-1",
            new CapturePaymentRequest(50000),
            "corr-3",
            CancellationToken.None);

        Assert.Equal(PaymentOperationOutcome.IdempotencyConflict, conflict.Outcome);
    }

    [Theory]
    [InlineData(PaymentRules.Pending)]
    [InlineData(PaymentRules.WaitingForCapture)]
    public async Task CancelAsync_CancelsPendingOrHoldPaymentAndRecordsPartyAndReason(string initialStatus)
    {
        var repository = new FakePaymentsRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = new PaymentCaptureService(repository, unitOfWork, new Sha256HashProvider());
        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 25000,
            Status = initialStatus,
            Capture = false
        };
        repository.Payments.Add(payment);

        var result = await service.CancelAsync(
            shopId,
            payment.Id,
            "cancel-key-1",
            new CancelPaymentRequest("out_of_stock"),
            "corr-1",
            CancellationToken.None);

        Assert.Equal(PaymentOperationOutcome.Success, result.Outcome);
        Assert.NotNull(result.Payment);
        Assert.Equal(PaymentRules.Canceled, result.Payment.Status);
        Assert.Equal(PaymentCancellationParties.Merchant, result.Payment.CancellationParty);
        Assert.Equal("out_of_stock", result.Payment.CancellationReason);
        Assert.Single(repository.OutboxEvents);
    }
}
