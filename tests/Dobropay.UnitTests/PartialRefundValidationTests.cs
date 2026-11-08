using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.Payments.Domain;
using Dobropay.Payments.Persistence;
using Dobropay.Payments.Provider;
using Dobropay.Payments.Services;

namespace Dobropay.UnitTests;

public sealed class PartialRefundValidationTests
{
    private readonly FakePaymentsRepository paymentsRepository = new();
    private readonly FakeRefundRepository refundRepository = new();
    private readonly FakeUnitOfWork unitOfWork = new();
    private readonly Sha256HashProvider hashProvider = new();
    private readonly PaymentProviderRouter router = new([new FakePaymentProvider()]);

    [Fact]
    public async Task CreateRefundAsync_SequentialPartialRefunds_AccuratelyTrackCapturedMinusRefundedAmount()
    {
        var service = new RefundService(paymentsRepository, refundRepository, router, unitOfWork, hashProvider);
        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 100000,
            CapturedAmount = 100000,
            RefundedAmount = 0,
            Status = PaymentRules.Succeeded,
            Capture = false
        };
        paymentsRepository.Payments.Add(payment);

        var first = await service.CreateRefundAsync(
            shopId,
            "refund-part-1",
            new CreateRefundRequest(payment.Id, 40000, DomainConstants.DefaultCurrency, "first part"),
            "corr-1",
            CancellationToken.None);

        Assert.Equal(RefundOperationOutcome.Success, first.Outcome);
        Assert.Equal(40000, payment.RefundedAmount);
        Assert.Equal(60000, payment.CapturedAmount - payment.RefundedAmount);

        var second = await service.CreateRefundAsync(
            shopId,
            "refund-part-2",
            new CreateRefundRequest(payment.Id, 60000, DomainConstants.DefaultCurrency, "second part"),
            "corr-2",
            CancellationToken.None);

        Assert.Equal(RefundOperationOutcome.Success, second.Outcome);
        Assert.Equal(100000, payment.RefundedAmount);
        Assert.Equal(0, payment.CapturedAmount - payment.RefundedAmount);

        var third = await service.CreateRefundAsync(
            shopId,
            "refund-part-3",
            new CreateRefundRequest(payment.Id, 1, DomainConstants.DefaultCurrency, "excess part"),
            "corr-3",
            CancellationToken.None);

        Assert.Equal(RefundOperationOutcome.AmountExceeded, third.Outcome);
        Assert.Equal(100000, payment.RefundedAmount);
        Assert.Equal(2, refundRepository.Refunds.Count);
    }

    [Fact]
    public async Task CreateRefundAsync_WhenRequestedAmountExceedsRemainingHold_ReturnsAmountExceeded()
    {
        var service = new RefundService(paymentsRepository, refundRepository, router, unitOfWork, hashProvider);
        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 75000,
            CapturedAmount = 50000,
            RefundedAmount = 0,
            Status = PaymentRules.Succeeded,
            Capture = false
        };
        paymentsRepository.Payments.Add(payment);

        var result = await service.CreateRefundAsync(
            shopId,
            "refund-excess",
            new CreateRefundRequest(payment.Id, 50001, DomainConstants.DefaultCurrency, "excess"),
            "corr-excess",
            CancellationToken.None);

        Assert.Equal(RefundOperationOutcome.AmountExceeded, result.Outcome);
        Assert.Equal(0, payment.RefundedAmount);
        Assert.Empty(refundRepository.Refunds);
    }

    [Theory]
    [InlineData(PaymentRules.Pending)]
    [InlineData(PaymentRules.WaitingForCapture)]
    [InlineData(PaymentRules.Canceled)]
    public async Task CreateRefundAsync_WhenPaymentStatusIsNotSucceeded_ReturnsInvalidState(string status)
    {
        var service = new RefundService(paymentsRepository, refundRepository, router, unitOfWork, hashProvider);
        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 50000,
            CapturedAmount = status == PaymentRules.WaitingForCapture ? 0 : null,
            RefundedAmount = 0,
            Status = status,
            Capture = false
        };
        paymentsRepository.Payments.Add(payment);

        var result = await service.CreateRefundAsync(
            shopId,
            "refund-bad-status",
            new CreateRefundRequest(payment.Id, 10000, DomainConstants.DefaultCurrency, "refund"),
            "corr-bad-status",
            CancellationToken.None);

        Assert.Equal(RefundOperationOutcome.InvalidState, result.Outcome);
        Assert.Empty(refundRepository.Refunds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    public async Task CreateRefundAsync_WithZeroOrNegativeAmount_ReturnsInvalidAmount(long invalidAmount)
    {
        var service = new RefundService(paymentsRepository, refundRepository, router, unitOfWork, hashProvider);
        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 50000,
            CapturedAmount = 50000,
            RefundedAmount = 0,
            Status = PaymentRules.Succeeded,
            Capture = false
        };
        paymentsRepository.Payments.Add(payment);

        var result = await service.CreateRefundAsync(
            shopId,
            "refund-zero-amount",
            new CreateRefundRequest(payment.Id, invalidAmount, DomainConstants.DefaultCurrency, "invalid amount"),
            "corr-zero-amount",
            CancellationToken.None);

        Assert.Equal(RefundOperationOutcome.InvalidAmount, result.Outcome);
        Assert.Empty(refundRepository.Refunds);
    }

    [Fact]
    public async Task CreateRefundAsync_WithMismatchedCurrency_ReturnsInvalidAmount()
    {
        var service = new RefundService(paymentsRepository, refundRepository, router, unitOfWork, hashProvider);
        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 50000,
            CapturedAmount = 50000,
            RefundedAmount = 0,
            Status = PaymentRules.Succeeded,
            Capture = false
        };
        paymentsRepository.Payments.Add(payment);

        var result = await service.CreateRefundAsync(
            shopId,
            "refund-currency",
            new CreateRefundRequest(payment.Id, 10000, "USD", "currency mismatch"),
            "corr-curr",
            CancellationToken.None);

        Assert.Equal(RefundOperationOutcome.InvalidAmount, result.Outcome);
        Assert.Empty(refundRepository.Refunds);
    }
}
