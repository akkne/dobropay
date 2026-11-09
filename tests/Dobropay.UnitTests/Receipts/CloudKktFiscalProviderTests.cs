using System.Net;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Receipts;
using Dobropay.Receipts.Models;
using Dobropay.Receipts.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dobropay.UnitTests.Receipts;

public sealed class CloudKktFiscalProviderTests
{
    private const string TestCloudKktUrl = "http://cloud-kkt.local:8080";
    private const string TestApiKey = "cloud-kkt-api-key";

    [Fact]
    public async Task RegisterReceiptAsync_MissingBaseUrl_ReturnsFailed()
    {
        var options = new ReceiptsOptions
        {
            EmailServiceBaseUrl = "http://email:8080",
            InternalServiceKey = "key",
            PaymentsServiceBaseUrl = "http://payments:8080",
            FiscalCloudKktBaseUrl = ""
        };

        var httpClientFactory = new TestHttpClientFactory(new HttpClient(new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))));
        var provider = new CloudKktFiscalProvider(httpClientFactory, options, NullLogger<CloudKktFiscalProvider>.Instance);

        var command = new FiscalRegistrationCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            ReceiptTypes.Payment,
            10000,
            DomainConstants.DefaultCurrency,
            DomainConstants.DefaultTaxSystemCode,
            null,
            null,
            Array.Empty<ReceiptItemDto>());

        var result = await provider.RegisterReceiptAsync(command, CancellationToken.None);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterReceiptAsync_SuccessfulApiResponse_ReturnsSucceededResult()
    {
        var options = new ReceiptsOptions
        {
            EmailServiceBaseUrl = "http://email:8080",
            InternalServiceKey = "key",
            PaymentsServiceBaseUrl = "http://payments:8080",
            FiscalCloudKktBaseUrl = TestCloudKktUrl,
            FiscalCloudKktApiKey = TestApiKey
        };

        var expectedJson = """
        {
            "fiscalDocumentNumber": 5055,
            "fiscalStorageNumber": "9999078900055555",
            "fiscalAttribute": "5555555555",
            "fiscalSign": "5555555555",
            "qrPayload": "t=20261108T1200&s=100.00&fn=9999078900055555&i=5055&fp=5555555555&n=1",
            "registeredAt": "2026-11-08T12:00:00Z"
        }
        """;

        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(expectedJson, System.Text.Encoding.UTF8, DomainConstants.ApplicationJsonContentType)
        });

        var httpClientFactory = new TestHttpClientFactory(new HttpClient(handler));
        var provider = new CloudKktFiscalProvider(httpClientFactory, options, NullLogger<CloudKktFiscalProvider>.Instance);

        var command = new FiscalRegistrationCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            ReceiptTypes.Payment,
            10000,
            DomainConstants.DefaultCurrency,
            DomainConstants.DefaultTaxSystemCode,
            "test@example.com",
            null,
            Array.Empty<ReceiptItemDto>());

        var result = await provider.RegisterReceiptAsync(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(5055, result.FiscalDocumentNumber);
        Assert.Equal("9999078900055555", result.FiscalStorageNumber);
        Assert.Equal("5555555555", result.FiscalAttribute);
        Assert.Contains("fn=9999078900055555", result.QrPayload);
    }

    [Fact]
    public async Task RegisterReceiptAsync_ErrorResponse_ReturnsFailedResult()
    {
        var options = new ReceiptsOptions
        {
            EmailServiceBaseUrl = "http://email:8080",
            InternalServiceKey = "key",
            PaymentsServiceBaseUrl = "http://payments:8080",
            FiscalCloudKktBaseUrl = TestCloudKktUrl,
            FiscalCloudKktApiKey = TestApiKey
        };

        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\":\"invalid_item\"}", System.Text.Encoding.UTF8, DomainConstants.ApplicationJsonContentType)
        });

        var httpClientFactory = new TestHttpClientFactory(new HttpClient(handler));
        var provider = new CloudKktFiscalProvider(httpClientFactory, options, NullLogger<CloudKktFiscalProvider>.Instance);

        var command = new FiscalRegistrationCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            ReceiptTypes.Payment,
            10000,
            DomainConstants.DefaultCurrency,
            DomainConstants.DefaultTaxSystemCode,
            null,
            null,
            Array.Empty<ReceiptItemDto>());

        var result = await provider.RegisterReceiptAsync(command, CancellationToken.None);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
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
