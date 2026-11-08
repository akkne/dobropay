using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.Payments.Domain;
using Dobropay.Payments.Persistence;
using Dobropay.Payments.Provider;
using Dobropay.Payments.Services;
using Microsoft.AspNetCore.Http;

namespace Dobropay.UnitTests;

public sealed class MutatingOperationsIdempotencyTests
{
    private readonly FakePaymentsRepository paymentsRepository = new();
    private readonly FakeRefundRepository refundRepository = new();
    private readonly FakeUnitOfWork unitOfWork = new();
    private readonly Sha256HashProvider hashProvider = new();

    [Fact]
    public async Task CreatePayment_ReplayWithSameIdempotencyKey_ReturnsSamePaymentWithoutDuplicates()
    {
        var shopId = Guid.NewGuid();
        var developerApiClient = new FakeDeveloperApiClient { ShopId = shopId };

        var tokenGenerator = new CheckoutTokenGenerator("checkout-secret", hashProvider);
        var sbpProvider = new SbpPaymentProvider("secret", hashProvider, new SbpQrGenerator());
        var cardProvider = new BankCardPaymentProvider("secret", "http://localhost", hashProvider);
        var cryptoProvider = new CryptoPaymentProvider("secret", hashProvider, new CryptoExchangeRateProvider());
        var cryptoRateProvider = new CryptoExchangeRateProvider();
        var router = new PaymentProviderRouter([sbpProvider, cardProvider, cryptoProvider, new SandboxProvider("secret", hashProvider)]);

        var options = new PaymentsOptions
        {
            DeveloperApiBaseUrl = "http://dev:8080",
            InternalServiceKey = "key",
            CheckoutSecret = "secret",
            ProviderSecret = "secret",
            SandboxEnabled = true,
            PublicBaseUrl = "http://localhost",
            InternalBaseUrl = "http://localhost"
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[HttpHeaderNames.ApiKey] = "test-key";
        httpContext.Request.Headers[HttpHeaderNames.IdempotencyKey] = "create-idemp-1";

        var body = new CreatePaymentRequest(50000, DomainConstants.DefaultCurrency, "order-123", true, PaymentMethodTypes.Sandbox, ConfirmationTypes.Redirect);

        var firstResult = await Dobropay.Payments.Api.PaymentsEndpoints.HandleCreatePaymentAsync(
            httpContext,
            body,
            developerApiClient,
            paymentsRepository,
            unitOfWork,
            hashProvider,
            tokenGenerator,
            router,
            sbpProvider,
            cardProvider,
            cryptoProvider,
            cryptoRateProvider,
            options,
            Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status201Created, GetStatusCode(firstResult));
        Assert.Single(paymentsRepository.Payments);
        var createdPayment = paymentsRepository.Payments[0];

        var secondResult = await Dobropay.Payments.Api.PaymentsEndpoints.HandleCreatePaymentAsync(
            httpContext,
            body,
            developerApiClient,
            paymentsRepository,
            unitOfWork,
            hashProvider,
            tokenGenerator,
            router,
            sbpProvider,
            cardProvider,
            cryptoProvider,
            cryptoRateProvider,
            options,
            Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, GetStatusCode(secondResult));
        Assert.Single(paymentsRepository.Payments);
        Assert.Equal(createdPayment.Id, paymentsRepository.Payments[0].Id);

        var conflictingBody = new CreatePaymentRequest(60000, DomainConstants.DefaultCurrency, "order-123", true, PaymentMethodTypes.Sandbox, ConfirmationTypes.Redirect);
        var conflictResult = await Dobropay.Payments.Api.PaymentsEndpoints.HandleCreatePaymentAsync(
            httpContext,
            conflictingBody,
            developerApiClient,
            paymentsRepository,
            unitOfWork,
            hashProvider,
            tokenGenerator,
            router,
            sbpProvider,
            cardProvider,
            cryptoProvider,
            cryptoRateProvider,
            options,
            Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status409Conflict, GetStatusCode(conflictResult));
    }

    [Fact]
    public async Task Capture_ReplayWithSameIdempotencyKey_ReturnsSamePaymentWithoutDuplicateCapture()
    {
        var service = new PaymentCaptureService(paymentsRepository, unitOfWork, hashProvider);
        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 10000,
            Status = PaymentRules.WaitingForCapture,
            Capture = false
        };
        paymentsRepository.Payments.Add(payment);

        var first = await service.CaptureAsync(
            shopId,
            payment.Id,
            "cap-idemp-1",
            new CapturePaymentRequest(8000),
            "corr-1",
            CancellationToken.None);

        Assert.Equal(PaymentOperationOutcome.Success, first.Outcome);
        Assert.Equal(PaymentRules.Succeeded, first.Payment?.Status);
        Assert.Equal(8000, first.Payment?.CapturedAmount);
        Assert.Single(paymentsRepository.OutboxEvents);

        var replay = await service.CaptureAsync(
            shopId,
            payment.Id,
            "cap-idemp-1",
            new CapturePaymentRequest(8000),
            "corr-2",
            CancellationToken.None);

        Assert.Equal(PaymentOperationOutcome.Success, replay.Outcome);
        Assert.Equal(8000, replay.Payment?.CapturedAmount);
        Assert.Single(paymentsRepository.OutboxEvents);

        var conflict = await service.CaptureAsync(
            shopId,
            payment.Id,
            "cap-idemp-1",
            new CapturePaymentRequest(10000),
            "corr-3",
            CancellationToken.None);

        Assert.Equal(PaymentOperationOutcome.IdempotencyConflict, conflict.Outcome);
    }

    [Fact]
    public async Task Cancel_ReplayWithSameIdempotencyKey_ReturnsSamePaymentWithoutDuplicateCancel()
    {
        var service = new PaymentCaptureService(paymentsRepository, unitOfWork, hashProvider);
        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 15000,
            Status = PaymentRules.WaitingForCapture,
            Capture = false
        };
        paymentsRepository.Payments.Add(payment);

        var first = await service.CancelAsync(
            shopId,
            payment.Id,
            "cancel-idemp-1",
            new CancelPaymentRequest("customer_request"),
            "corr-1",
            CancellationToken.None);

        Assert.Equal(PaymentOperationOutcome.Success, first.Outcome);
        Assert.Equal(PaymentRules.Canceled, first.Payment?.Status);
        Assert.Equal(PaymentCancellationParties.Merchant, first.Payment?.CancellationParty);
        Assert.Equal("customer_request", first.Payment?.CancellationReason);
        Assert.Single(paymentsRepository.OutboxEvents);

        var replay = await service.CancelAsync(
            shopId,
            payment.Id,
            "cancel-idemp-1",
            new CancelPaymentRequest("customer_request"),
            "corr-2",
            CancellationToken.None);

        Assert.Equal(PaymentOperationOutcome.Success, replay.Outcome);
        Assert.Equal(PaymentRules.Canceled, replay.Payment?.Status);
        Assert.Single(paymentsRepository.OutboxEvents);

        var conflict = await service.CancelAsync(
            shopId,
            payment.Id,
            "cancel-idemp-1",
            new CancelPaymentRequest("different_reason"),
            "corr-3",
            CancellationToken.None);

        Assert.Equal(PaymentOperationOutcome.IdempotencyConflict, conflict.Outcome);
    }

    [Fact]
    public async Task Refund_ReplayWithSameIdempotencyKey_ReturnsSameRefundWithoutDuplicateDeduction()
    {
        var provider = new FakePaymentProvider();
        var router = new PaymentProviderRouter([provider]);
        var service = new RefundService(paymentsRepository, refundRepository, router, unitOfWork, hashProvider);

        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 50000,
            CapturedAmount = 50000,
            Status = PaymentRules.Succeeded,
            Capture = false
        };
        paymentsRepository.Payments.Add(payment);

        var first = await service.CreateRefundAsync(
            shopId,
            "ref-idemp-1",
            new CreateRefundRequest(payment.Id, 20000, DomainConstants.DefaultCurrency, "refund-1"),
            "corr-1",
            CancellationToken.None);

        Assert.Equal(RefundOperationOutcome.Success, first.Outcome);
        Assert.True(first.Created);
        Assert.Equal(20000, payment.RefundedAmount);
        Assert.Single(refundRepository.Refunds);

        var replay = await service.CreateRefundAsync(
            shopId,
            "ref-idemp-1",
            new CreateRefundRequest(payment.Id, 20000, DomainConstants.DefaultCurrency, "refund-1"),
            "corr-2",
            CancellationToken.None);

        Assert.Equal(RefundOperationOutcome.Success, replay.Outcome);
        Assert.False(replay.Created);
        Assert.Equal(first.Refund?.Id, replay.Refund?.Id);
        Assert.Equal(20000, payment.RefundedAmount);
        Assert.Single(refundRepository.Refunds);

        var conflict = await service.CreateRefundAsync(
            shopId,
            "ref-idemp-1",
            new CreateRefundRequest(payment.Id, 30000, DomainConstants.DefaultCurrency, "different_amount"),
            "corr-3",
            CancellationToken.None);

        Assert.Equal(RefundOperationOutcome.IdempotencyConflict, conflict.Outcome);
        Assert.Equal(20000, payment.RefundedAmount);
    }

    private static int? GetStatusCode(IResult result)
    {
        if (result is IStatusCodeHttpResult statusCodeResult)
        {
            return statusCodeResult.StatusCode;
        }

        return null;
    }
}
