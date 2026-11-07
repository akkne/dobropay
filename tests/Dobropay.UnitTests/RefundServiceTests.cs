using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.Payments.Domain;
using Dobropay.Payments.Persistence;
using Dobropay.Payments.Services;

namespace Dobropay.UnitTests;

public sealed class RefundServiceTests
{
    [Fact]
    public async Task CreateRefundAsync_AllowsMultiplePartialRefundsUpToCapturedAmountAndRejectsExcess()
    {
        var paymentsRepository = new FakePaymentsRepository();
        var refundRepository = new FakeRefundRepository();
        var provider = new FakePaymentProvider();
        var unitOfWork = new FakeUnitOfWork();
        var service = new RefundService(
            paymentsRepository,
            refundRepository,
            provider,
            unitOfWork,
            new Sha256HashProvider());

        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 100000,
            CapturedAmount = 80000,
            Status = PaymentRules.Succeeded,
            Capture = false
        };
        paymentsRepository.Payments.Add(payment);

        var first = await service.CreateRefundAsync(
            shopId,
            "ref-key-1",
            new CreateRefundRequest(payment.Id, 30000, DomainConstants.DefaultCurrency, "partial-1"),
            "corr-1",
            CancellationToken.None);

        Assert.Equal(RefundOperationOutcome.Success, first.Outcome);
        Assert.True(first.Created);
        Assert.Equal(30000, payment.RefundedAmount);

        var replay = await service.CreateRefundAsync(
            shopId,
            "ref-key-1",
            new CreateRefundRequest(payment.Id, 30000, DomainConstants.DefaultCurrency, "partial-1"),
            "corr-1b",
            CancellationToken.None);

        Assert.Equal(RefundOperationOutcome.Success, replay.Outcome);
        Assert.False(replay.Created);
        Assert.Equal(first.Refund?.Id, replay.Refund?.Id);
        Assert.Equal(30000, payment.RefundedAmount);

        var second = await service.CreateRefundAsync(
            shopId,
            "ref-key-2",
            new CreateRefundRequest(payment.Id, 50000, DomainConstants.DefaultCurrency, "partial-2"),
            "corr-2",
            CancellationToken.None);

        Assert.Equal(RefundOperationOutcome.Success, second.Outcome);
        Assert.Equal(80000, payment.RefundedAmount);

        var exceeded = await service.CreateRefundAsync(
            shopId,
            "ref-key-3",
            new CreateRefundRequest(payment.Id, 1, DomainConstants.DefaultCurrency, "exceed"),
            "corr-3",
            CancellationToken.None);

        Assert.Equal(RefundOperationOutcome.AmountExceeded, exceeded.Outcome);
        Assert.Equal(2, refundRepository.Refunds.Count);
    }
}
