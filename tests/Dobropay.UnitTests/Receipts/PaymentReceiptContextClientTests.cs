using System.Net;
using System.Text.Json;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Receipts;
using Dobropay.Receipts.Models;
using Dobropay.Receipts.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dobropay.UnitTests.Receipts;

public sealed class PaymentReceiptContextClientTests
{
    private const string ExpectedInternalKey = "secret-key-123";
    private const string PaymentsBaseUrl = "http://payments.local:8080";

    [Fact]
    public async Task GetReceiptContextAsync_Success_ReturnsDtoAndSendsHeaders()
    {
        var shopId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var refundId = Guid.NewGuid();

        var expectedDto = new PaymentReceiptContextDto(
            shopId,
            paymentId,
            refundId,
            ReceiptTypes.Refund,
            5000,
            DomainConstants.DefaultCurrency,
            "order-42",
            PaymentMethodTypes.BankCard,
            DomainConstants.DefaultTaxSystemCode,
            "buyer@example.com",
            null,
            new List<ReceiptItemDto>
            {
                new("Книга", "1", 5000, DomainConstants.DefaultVatCode)
            });

        HttpRequestMessage? capturedRequest = null;
        var handler = new TestHttpMessageHandler(request =>
        {
            capturedRequest = request;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(expectedDto, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                    System.Text.Encoding.UTF8,
                    DomainConstants.ApplicationJsonContentType)
            };
            return response;
        });

        var httpClientFactory = new TestHttpClientFactory(new HttpClient(handler));
        var options = new ReceiptsOptions
        {
            EmailServiceBaseUrl = "http://email:8080",
            InternalServiceKey = ExpectedInternalKey,
            PaymentsServiceBaseUrl = PaymentsBaseUrl
        };

        var client = new PaymentReceiptContextClient(
            httpClientFactory,
            options,
            NullLogger<PaymentReceiptContextClient>.Instance);

        var result = await client.GetReceiptContextAsync(shopId, paymentId, refundId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(shopId, result.ShopId);
        Assert.Equal(paymentId, result.PaymentId);
        Assert.Equal(refundId, result.RefundId);
        Assert.Equal(5000, result.Amount);
        Assert.Single(result.Items);

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest.Method);
        Assert.Equal(
            $"{PaymentsBaseUrl}/internal/v1/shops/{shopId}/payments/{paymentId}/receipt-context?refund_id={refundId}",
            capturedRequest.RequestUri?.ToString());
        Assert.True(capturedRequest.Headers.Contains(HttpHeaderNames.InternalKey));
        Assert.Equal(ExpectedInternalKey, capturedRequest.Headers.GetValues(HttpHeaderNames.InternalKey).Single());
    }

    [Fact]
    public async Task GetReceiptContextAsync_NotFound_ReturnsNull()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var httpClientFactory = new TestHttpClientFactory(new HttpClient(handler));
        var options = new ReceiptsOptions
        {
            EmailServiceBaseUrl = "http://email:8080",
            InternalServiceKey = ExpectedInternalKey,
            PaymentsServiceBaseUrl = PaymentsBaseUrl
        };

        var client = new PaymentReceiptContextClient(
            httpClientFactory,
            options,
            NullLogger<PaymentReceiptContextClient>.Instance);

        var result = await client.GetReceiptContextAsync(Guid.NewGuid(), Guid.NewGuid(), null, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReceiptContextAsync_InternalServerError_ReturnsNull()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var httpClientFactory = new TestHttpClientFactory(new HttpClient(handler));
        var options = new ReceiptsOptions
        {
            EmailServiceBaseUrl = "http://email:8080",
            InternalServiceKey = ExpectedInternalKey,
            PaymentsServiceBaseUrl = PaymentsBaseUrl
        };

        var client = new PaymentReceiptContextClient(
            httpClientFactory,
            options,
            NullLogger<PaymentReceiptContextClient>.Instance);

        var result = await client.GetReceiptContextAsync(Guid.NewGuid(), Guid.NewGuid(), null, CancellationToken.None);

        Assert.Null(result);
    }

    private sealed class TestHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> sendFunc) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(sendFunc(request));
    }
}
