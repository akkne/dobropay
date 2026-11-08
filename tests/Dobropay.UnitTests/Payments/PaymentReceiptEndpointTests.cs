using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Receipts;
using Dobropay.Contracts.Security;
using Dobropay.Payments.Api;
using Dobropay.Payments.Domain;
using Dobropay.Payments.Persistence;
using Dobropay.Payments.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Dobropay.UnitTests.Payments;

public sealed class PaymentReceiptEndpointTests
{
    private const string ExpectedInternalKey = "expected-internal-key";

    private readonly IInternalKeyValidator _keyValidator =
        new InternalKeyValidator(ExpectedInternalKey, new Sha256HashProvider());

    [Fact]
    public async Task HandleReceiptContextAsync_MissingOrInvalidInternalKey_ReturnsUnauthorized()
    {
        var paymentsRepository = new FakePaymentsRepository();
        var refundRepository = new FakeRefundRepository();
        var contextService = new PaymentReceiptContextService(paymentsRepository, refundRepository);

        var httpContext = new DefaultHttpContext();
        var resultMissing = await PaymentsEndpoints.HandleReceiptContextAsync(
            httpContext,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            _keyValidator,
            contextService,
            CancellationToken.None);

        Assert.IsType<UnauthorizedHttpResult>(resultMissing);

        httpContext.Request.Headers[HttpHeaderNames.InternalKey] = "wrong-key";
        var resultInvalid = await PaymentsEndpoints.HandleReceiptContextAsync(
            httpContext,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            _keyValidator,
            contextService,
            CancellationToken.None);

        Assert.IsType<UnauthorizedHttpResult>(resultInvalid);
    }

    [Fact]
    public async Task HandleReceiptContextAsync_ValidInternalKey_ReturnsOkWithContext()
    {
        var paymentsRepository = new FakePaymentsRepository();
        var refundRepository = new FakeRefundRepository();
        var contextService = new PaymentReceiptContextService(paymentsRepository, refundRepository);

        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 10000,
            Currency = DomainConstants.DefaultCurrency,
            ExternalOrderId = "ORDER-ENDP-1",
            PaymentMethodType = PaymentMethodTypes.Sandbox,
            Status = PaymentRules.Succeeded
        };
        paymentsRepository.Payments.Add(payment);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[HttpHeaderNames.InternalKey] = ExpectedInternalKey;

        var result = await PaymentsEndpoints.HandleReceiptContextAsync(
            httpContext,
            shopId,
            payment.Id,
            null,
            _keyValidator,
            contextService,
            CancellationToken.None);

        var okResult = Assert.IsType<Ok<PaymentReceiptContextDto>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal(payment.Id, okResult.Value.PaymentId);
        Assert.Equal(shopId, okResult.Value.ShopId);
        Assert.Equal(ReceiptTypes.Payment, okResult.Value.ReceiptType);
        Assert.Equal(10000, okResult.Value.Amount);
    }

    [Fact]
    public async Task HandleReceiptContextAsync_WithRefundIdInQuery_PassesRefundIdToService()
    {
        var paymentsRepository = new FakePaymentsRepository();
        var refundRepository = new FakeRefundRepository();
        var contextService = new PaymentReceiptContextService(paymentsRepository, refundRepository);

        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 50000,
            Currency = DomainConstants.DefaultCurrency,
            ExternalOrderId = "ORDER-QUERY-REF",
            PaymentMethodType = PaymentMethodTypes.Sandbox,
            Status = PaymentRules.Succeeded
        };
        paymentsRepository.Payments.Add(payment);

        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            PaymentId = payment.Id,
            Amount = 20000,
            Currency = DomainConstants.DefaultCurrency,
            Status = RefundStatuses.Succeeded
        };
        refundRepository.Refunds.Add(refund);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[HttpHeaderNames.InternalKey] = ExpectedInternalKey;
        httpContext.Request.QueryString = new QueryString($"?refund_id={refund.Id}");

        var result = await PaymentsEndpoints.HandleReceiptContextAsync(
            httpContext,
            shopId,
            payment.Id,
            null,
            _keyValidator,
            contextService,
            CancellationToken.None);

        var okResult = Assert.IsType<Ok<PaymentReceiptContextDto>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal(refund.Id, okResult.Value.RefundId);
        Assert.Equal(ReceiptTypes.Refund, okResult.Value.ReceiptType);
        Assert.Equal(20000, okResult.Value.Amount);
    }

    [Fact]
    public async Task HandleReceiptContextAsync_PaymentNotFound_ReturnsNotFound()
    {
        var paymentsRepository = new FakePaymentsRepository();
        var refundRepository = new FakeRefundRepository();
        var contextService = new PaymentReceiptContextService(paymentsRepository, refundRepository);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[HttpHeaderNames.InternalKey] = ExpectedInternalKey;

        var result = await PaymentsEndpoints.HandleReceiptContextAsync(
            httpContext,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            _keyValidator,
            contextService,
            CancellationToken.None);

        Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        var statusResult = (IStatusCodeHttpResult)result;
        Assert.Equal(StatusCodes.Status404NotFound, statusResult.StatusCode);
    }

    [Fact]
    public async Task HandleReceiptContextAsync_RefundNotFound_ReturnsNotFound()
    {
        var paymentsRepository = new FakePaymentsRepository();
        var refundRepository = new FakeRefundRepository();
        var contextService = new PaymentReceiptContextService(paymentsRepository, refundRepository);

        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 50000,
            Status = PaymentRules.Succeeded
        };
        paymentsRepository.Payments.Add(payment);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[HttpHeaderNames.InternalKey] = ExpectedInternalKey;

        var result = await PaymentsEndpoints.HandleReceiptContextAsync(
            httpContext,
            shopId,
            payment.Id,
            Guid.NewGuid(),
            _keyValidator,
            contextService,
            CancellationToken.None);

        Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        var statusResult = (IStatusCodeHttpResult)result;
        Assert.Equal(StatusCodes.Status404NotFound, statusResult.StatusCode);
    }
}
