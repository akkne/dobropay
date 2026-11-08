using System.Text.Json;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Receipts;
using Dobropay.Contracts.Security;
using Dobropay.Payments.Api;
using Dobropay.Payments.Domain;
using Dobropay.Payments.Persistence;
using Dobropay.Payments.Provider;
using Dobropay.Payments.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dobropay.UnitTests.Payments;

public sealed class PaymentReceiptPayloadPersistenceTests
{
    private const string TestInternalKey = "test-internal-key";
    private const string TestProviderSecret = "test-provider-secret";
    private const string TestPublicBaseUrl = "http://localhost:8080";

    [Fact]
    public async Task HandleCreatePaymentAsync_WithReceipt_SavesPaymentReceiptPayload()
    {
        var hashProvider = new Sha256HashProvider();
        var repository = new FakePaymentsRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tokenGenerator = new CheckoutTokenGenerator("token-secret", hashProvider);
        var sandboxProvider = new SandboxProvider(TestProviderSecret, hashProvider);
        var sbpProvider = new SbpPaymentProvider(TestProviderSecret, hashProvider, new SbpQrGenerator());
        var cardProvider = new BankCardPaymentProvider(TestProviderSecret, TestPublicBaseUrl, hashProvider);
        var cryptoProvider = new CryptoPaymentProvider(TestProviderSecret, hashProvider, new CryptoExchangeRateProvider());
        var cryptoRateProvider = new CryptoExchangeRateProvider();
        var router = new PaymentProviderRouter([sandboxProvider, sbpProvider, cardProvider, cryptoProvider]);
        var developerApiClient = new FakeDeveloperApiClient();
        var options = new PaymentsOptions
        {
            DeveloperApiBaseUrl = "http://localhost",
            InternalServiceKey = TestInternalKey,
            CheckoutSecret = "secret",
            ProviderSecret = TestProviderSecret,
            SandboxEnabled = true,
            PublicBaseUrl = TestPublicBaseUrl,
            InternalBaseUrl = "http://localhost:8081"
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[HttpHeaderNames.ApiKey] = "dp_live_test";
        httpContext.Request.Headers[HttpHeaderNames.IdempotencyKey] = "idem-pay-receipt-1";

        var receiptItems = new List<ReceiptItemDto>
        {
            new("Товар 1", "2.000", 60000, 1),
            new("Доставка", "1.000", 40000, 1)
        };
        var receipt = new ReceiptRequestDto(
            new ReceiptCustomerDto("buyer@dobropay.test", "+79990001122"),
            receiptItems,
            2);

        var request = new CreatePaymentRequest(
            Amount: 100000,
            Currency: DomainConstants.DefaultCurrency,
            ExternalOrderId: "ORDER-RECEIPT-01",
            Capture: true,
            Receipt: receipt);

        var result = await PaymentsEndpoints.HandleCreatePaymentAsync(
            httpContext,
            request,
            developerApiClient,
            repository,
            unitOfWork,
            hashProvider,
            tokenGenerator,
            router,
            sbpProvider,
            cardProvider,
            cryptoProvider,
            cryptoRateProvider,
            options,
            NullLogger.Instance,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(repository.ReceiptPayloads);

        var payload = repository.ReceiptPayloads[0];
        Assert.Equal(developerApiClient.ShopId, payload.ShopId);
        Assert.Single(repository.Payments);
        Assert.Equal(repository.Payments[0].Id, payload.PaymentId);
        Assert.Null(payload.RefundId);
        Assert.Equal(ReceiptTypes.Payment, payload.ReceiptType);
        Assert.Equal(2, payload.TaxSystemCode);
        Assert.Equal("buyer@dobropay.test", payload.CustomerEmail);
        Assert.Equal("+79990001122", payload.CustomerPhone);

        var deserializedItems = JsonSerializer.Deserialize<IReadOnlyList<ReceiptItemDto>>(payload.ItemsJson);
        Assert.NotNull(deserializedItems);
        Assert.Equal(2, deserializedItems.Count);
        Assert.Equal("Товар 1", deserializedItems[0].Description);
        Assert.Equal("2.000", deserializedItems[0].Quantity);
        Assert.Equal(60000, deserializedItems[0].Amount);
    }

    [Fact]
    public async Task HandleCreatePaymentAsync_WithoutReceipt_DoesNotSavePaymentReceiptPayload()
    {
        var hashProvider = new Sha256HashProvider();
        var repository = new FakePaymentsRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tokenGenerator = new CheckoutTokenGenerator("token-secret", hashProvider);
        var sandboxProvider = new SandboxProvider(TestProviderSecret, hashProvider);
        var sbpProvider = new SbpPaymentProvider(TestProviderSecret, hashProvider, new SbpQrGenerator());
        var cardProvider = new BankCardPaymentProvider(TestProviderSecret, TestPublicBaseUrl, hashProvider);
        var cryptoProvider = new CryptoPaymentProvider(TestProviderSecret, hashProvider, new CryptoExchangeRateProvider());
        var cryptoRateProvider = new CryptoExchangeRateProvider();
        var router = new PaymentProviderRouter([sandboxProvider, sbpProvider, cardProvider, cryptoProvider]);
        var developerApiClient = new FakeDeveloperApiClient();
        var options = new PaymentsOptions
        {
            DeveloperApiBaseUrl = "http://localhost",
            InternalServiceKey = TestInternalKey,
            CheckoutSecret = "secret",
            ProviderSecret = TestProviderSecret,
            SandboxEnabled = true,
            PublicBaseUrl = TestPublicBaseUrl,
            InternalBaseUrl = "http://localhost:8081"
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[HttpHeaderNames.ApiKey] = "dp_live_test";
        httpContext.Request.Headers[HttpHeaderNames.IdempotencyKey] = "idem-pay-no-receipt-1";

        var request = new CreatePaymentRequest(
            Amount: 50000,
            Currency: DomainConstants.DefaultCurrency,
            ExternalOrderId: "ORDER-NO-RECEIPT",
            Capture: true);

        var result = await PaymentsEndpoints.HandleCreatePaymentAsync(
            httpContext,
            request,
            developerApiClient,
            repository,
            unitOfWork,
            hashProvider,
            tokenGenerator,
            router,
            sbpProvider,
            cardProvider,
            cryptoProvider,
            cryptoRateProvider,
            options,
            NullLogger.Instance,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(repository.ReceiptPayloads);
        Assert.Single(repository.Payments);
    }

    [Fact]
    public async Task CreateRefundAsync_WithReceipt_SavesRefundReceiptPayload()
    {
        var hashProvider = new Sha256HashProvider();
        var paymentsRepository = new FakePaymentsRepository();
        var refundRepository = new FakeRefundRepository();
        var provider = new FakePaymentProvider();
        var router = new PaymentProviderRouter([provider]);
        var unitOfWork = new FakeUnitOfWork();
        var service = new RefundService(
            paymentsRepository,
            refundRepository,
            router,
            unitOfWork,
            hashProvider);

        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 100000,
            CapturedAmount = 100000,
            Status = PaymentRules.Succeeded,
            Capture = true
        };
        paymentsRepository.Payments.Add(payment);

        var refundItems = new List<ReceiptItemDto>
        {
            new("Возврат позиции 1", "1.000", 40000, 1)
        };
        var receipt = new ReceiptRequestDto(
            new ReceiptCustomerDto("refund-client@dobropay.test", "+79998887766"),
            refundItems,
            1);

        var refundRequest = new CreateRefundRequest(
            PaymentId: payment.Id,
            Amount: 40000,
            Currency: DomainConstants.DefaultCurrency,
            Reason: "customer_return",
            Receipt: receipt);

        var result = await service.CreateRefundAsync(
            shopId,
            "ref-idem-1",
            refundRequest,
            "corr-ref-1",
            CancellationToken.None);

        Assert.Equal(RefundOperationOutcome.Success, result.Outcome);
        Assert.NotNull(result.Refund);
        Assert.Single(paymentsRepository.ReceiptPayloads);

        var payload = paymentsRepository.ReceiptPayloads[0];
        Assert.Equal(shopId, payload.ShopId);
        Assert.Equal(payment.Id, payload.PaymentId);
        Assert.Equal(result.Refund.Id, payload.RefundId);
        Assert.Equal(ReceiptTypes.Refund, payload.ReceiptType);
        Assert.Equal(1, payload.TaxSystemCode);
        Assert.Equal("refund-client@dobropay.test", payload.CustomerEmail);
        Assert.Equal("+79998887766", payload.CustomerPhone);

        var deserializedItems = JsonSerializer.Deserialize<IReadOnlyList<ReceiptItemDto>>(payload.ItemsJson);
        Assert.NotNull(deserializedItems);
        Assert.Single(deserializedItems);
        Assert.Equal("Возврат позиции 1", deserializedItems[0].Description);
        Assert.Equal(40000, deserializedItems[0].Amount);
    }

    [Fact]
    public async Task CreateRefundAsync_WithoutReceipt_DoesNotSaveRefundReceiptPayload()
    {
        var hashProvider = new Sha256HashProvider();
        var paymentsRepository = new FakePaymentsRepository();
        var refundRepository = new FakeRefundRepository();
        var provider = new FakePaymentProvider();
        var router = new PaymentProviderRouter([provider]);
        var unitOfWork = new FakeUnitOfWork();
        var service = new RefundService(
            paymentsRepository,
            refundRepository,
            router,
            unitOfWork,
            hashProvider);

        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 100000,
            CapturedAmount = 100000,
            Status = PaymentRules.Succeeded,
            Capture = true
        };
        paymentsRepository.Payments.Add(payment);

        var refundRequest = new CreateRefundRequest(
            PaymentId: payment.Id,
            Amount: 20000,
            Currency: DomainConstants.DefaultCurrency,
            Reason: "no_receipt");

        var result = await service.CreateRefundAsync(
            shopId,
            "ref-idem-no-receipt",
            refundRequest,
            "corr-ref-2",
            CancellationToken.None);

        Assert.Equal(RefundOperationOutcome.Success, result.Outcome);
        Assert.Empty(paymentsRepository.ReceiptPayloads);
    }

    [Fact]
    public async Task CreateRefundAsync_IdempotentReplayWithReceipt_DoesNotDuplicatePayload()
    {
        var hashProvider = new Sha256HashProvider();
        var paymentsRepository = new FakePaymentsRepository();
        var refundRepository = new FakeRefundRepository();
        var provider = new FakePaymentProvider();
        var router = new PaymentProviderRouter([provider]);
        var unitOfWork = new FakeUnitOfWork();
        var service = new RefundService(
            paymentsRepository,
            refundRepository,
            router,
            unitOfWork,
            hashProvider);

        var shopId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Amount = 80000,
            CapturedAmount = 80000,
            Status = PaymentRules.Succeeded,
            Capture = true
        };
        paymentsRepository.Payments.Add(payment);

        var receipt = new ReceiptRequestDto(
            new ReceiptCustomerDto("replay@dobropay.test", null),
            [new ReceiptItemDto("Возврат", "1.000", 30000)],
            1);

        var refundRequest = new CreateRefundRequest(
            payment.Id,
            30000,
            DomainConstants.DefaultCurrency,
            "replay",
            receipt);

        var first = await service.CreateRefundAsync(shopId, "idem-replay-1", refundRequest, "c1", CancellationToken.None);
        var second = await service.CreateRefundAsync(shopId, "idem-replay-1", refundRequest, "c2", CancellationToken.None);

        Assert.Equal(RefundOperationOutcome.Success, first.Outcome);
        Assert.True(first.Created);
        Assert.Equal(RefundOperationOutcome.Success, second.Outcome);
        Assert.False(second.Created);
        Assert.Single(paymentsRepository.ReceiptPayloads);
    }
}
