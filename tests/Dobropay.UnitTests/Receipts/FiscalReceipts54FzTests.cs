using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Receipts;
using Dobropay.Receipts.Models;
using Dobropay.Receipts.Services;
using Dobropay.UnitTests.MerchantAdmin;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dobropay.UnitTests.Receipts;

public sealed class FiscalReceipts54FzTests
{
    [Fact]
    public async Task SandboxFiscalProvider_GeneratesValid54FzRequisitesAndMandatoryQrFields()
    {
        var provider = new SandboxFiscalProvider();
        var command = new FiscalRegistrationCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            ReceiptTypes.Payment,
            245050,
            DomainConstants.DefaultCurrency,
            DomainConstants.DefaultTaxSystemCode,
            "client@example.com",
            "+79990001122",
            new List<ReceiptItemDto>
            {
                new(
                    "Подписка на сервис",
                    "1",
                    145050,
                    VatCode: 1,
                    PaymentMode: FiscalPaymentModes.FullPayment,
                    PaymentSubject: FiscalPaymentSubjects.Service),
                new(
                    "Фирменный сувенир",
                    "1",
                    100000,
                    VatCode: 2,
                    PaymentMode: FiscalPaymentModes.FullPayment,
                    PaymentSubject: FiscalPaymentSubjects.Commodity)
            });

        var result = await provider.RegisterReceiptAsync(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.FiscalDocumentNumber > 0);
        Assert.Matches(@"^\d{16}$", result.FiscalStorageNumber);
        Assert.Matches(@"^\d{10}$", result.FiscalAttribute);
        Assert.Equal(result.FiscalAttribute, result.FiscalSign);

        Assert.Contains("s=2450.50", result.QrPayload);
        Assert.Contains($"fn={result.FiscalStorageNumber}", result.QrPayload);
        Assert.Contains($"i={result.FiscalDocumentNumber}", result.QrPayload);
        Assert.Contains($"fp={result.FiscalAttribute}", result.QrPayload);
        Assert.Contains("&n=1", result.QrPayload);
    }

    [Fact]
    public async Task SandboxFiscalProvider_ForRefund_SetsOperationTypeTwoInQrCode()
    {
        var provider = new SandboxFiscalProvider();
        var command = new FiscalRegistrationCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ReceiptTypes.Refund,
            50000,
            DomainConstants.DefaultCurrency,
            DomainConstants.DefaultTaxSystemCode,
            "refund-client@example.com",
            null,
            new List<ReceiptItemDto>
            {
                new(
                    "Возврат оплаты",
                    "1",
                    50000,
                    VatCode: 1,
                    PaymentMode: FiscalPaymentModes.FullPayment,
                    PaymentSubject: FiscalPaymentSubjects.Service)
            });

        var result = await provider.RegisterReceiptAsync(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("s=500.00", result.QrPayload);
        Assert.Contains("&n=2", result.QrPayload);
    }

    [Fact]
    public async Task CloudKktFiscalProvider_SendsVatAndItemAttributes_Parses54FzResponse()
    {
        string? capturedRequestBody = null;
        var handler = new TestHttpMessageHandler(async (request, ct) =>
        {
            if (request.Content is not null)
            {
                capturedRequestBody = await request.Content.ReadAsStringAsync(ct);
            }

            var responseJson = """
            {
                "fiscalDocumentNumber": 12345,
                "fiscalStorageNumber": "9999078900099999",
                "fiscalAttribute": "1234567890",
                "fiscalSign": "1234567890",
                "qrPayload": "t=20261108T1500&s=150.00&fn=9999078900099999&i=12345&fp=1234567890&n=1",
                "registeredAt": "2026-11-08T15:00:00Z"
            }
            """;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, DomainConstants.ApplicationJsonContentType)
            };
        });

        var httpClientFactory = new TestHttpClientFactory(new HttpClient(handler));
        var options = new ReceiptsOptions
        {
            EmailServiceBaseUrl = "http://email:8080",
            InternalServiceKey = "test-key",
            DemoReceiptEmail = "demo@example.com",
            PaymentsServiceBaseUrl = "http://payments:8080",
            FiscalCloudKktBaseUrl = "http://cloud-kkt:8080",
            FiscalCloudKktApiKey = "cloud-api-key"
        };

        var provider = new CloudKktFiscalProvider(httpClientFactory, options, NullLogger<CloudKktFiscalProvider>.Instance);
        var command = new FiscalRegistrationCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            ReceiptTypes.Payment,
            15000,
            DomainConstants.DefaultCurrency,
            DomainConstants.DefaultTaxSystemCode,
            "buyer@example.com",
            null,
            new List<ReceiptItemDto>
            {
                new(
                    "Цифровая услуга",
                    "1",
                    15000,
                    VatCode: 2,
                    PaymentMode: FiscalPaymentModes.FullPayment,
                    PaymentSubject: FiscalPaymentSubjects.Service)
            });

        var result = await provider.RegisterReceiptAsync(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(12345, result.FiscalDocumentNumber);
        Assert.Equal("9999078900099999", result.FiscalStorageNumber);
        Assert.Equal("1234567890", result.FiscalAttribute);
        Assert.NotNull(capturedRequestBody);

        using var jsonDoc = JsonDocument.Parse(capturedRequestBody);
        var root = jsonDoc.RootElement;
        var firstItem = root.GetProperty("items")[0];
        Assert.Equal("Цифровая услуга", firstItem.GetProperty("description").GetString());
        Assert.Equal(2, firstItem.GetProperty("vatCode").GetInt32());
        Assert.Equal(FiscalPaymentModes.FullPayment, firstItem.GetProperty("paymentMode").GetString());
        Assert.Equal(FiscalPaymentSubjects.Service, firstItem.GetProperty("paymentSubject").GetString());
    }
}
