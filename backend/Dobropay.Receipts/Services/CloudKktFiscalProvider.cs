using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dobropay.Contracts.Constants;
using Dobropay.Receipts.Models;

namespace Dobropay.Receipts.Services;

public sealed class CloudKktFiscalProvider(
    IHttpClientFactory httpClientFactory,
    ReceiptsOptions options,
    ILogger<CloudKktFiscalProvider> logger) : IFiscalProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<FiscalRegistrationResult> RegisterReceiptAsync(
        FiscalRegistrationCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.FiscalCloudKktBaseUrl))
        {
            logger.LogWarning("Cloud KKT base URL is not configured. Falling back to failure.");
            return FiscalRegistrationResult.Failed("Cloud KKT base URL not configured");
        }

        try
        {
            var client = httpClientFactory.CreateClient();
            if (!string.IsNullOrWhiteSpace(options.FiscalCloudKktApiKey))
            {
                client.DefaultRequestHeaders.Add(HttpHeaderNames.InternalKey, options.FiscalCloudKktApiKey);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.FiscalCloudKktApiKey);
            }

            var url = options.FiscalCloudKktBaseUrl.TrimEnd('/') + "/api/v1/receipts";
            var payload = new
            {
                receipt_id = command.ReceiptId,
                shop_id = command.ShopId,
                payment_id = command.PaymentId,
                refund_id = command.RefundId,
                receipt_type = command.ReceiptType,
                total_amount = command.TotalAmount,
                currency = command.Currency,
                tax_system_code = command.TaxSystemCode,
                customer_email = command.CustomerEmail,
                customer_phone = command.CustomerPhone,
                items = command.Items
            };

            using var response = await client.PostAsJsonAsync(url, payload, JsonOptions, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError("Cloud KKT returned {StatusCode}: {ErrorText}", response.StatusCode, errorText);
                return FiscalRegistrationResult.Failed($"Cloud KKT error: {response.StatusCode}");
            }

            var doc = await response.Content.ReadFromJsonAsync<CloudKktResponse>(JsonOptions, cancellationToken);
            if (doc is null)
            {
                return FiscalRegistrationResult.Failed("Cloud KKT returned empty response");
            }

            return FiscalRegistrationResult.Succeeded(
                doc.FiscalDocumentNumber,
                doc.FiscalStorageNumber,
                doc.FiscalAttribute,
                doc.FiscalSign,
                doc.QrPayload,
                doc.RegisteredAt ?? DateTimeOffset.UtcNow);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to register receipt {ReceiptId} with Cloud KKT", command.ReceiptId);
            return FiscalRegistrationResult.Failed(exception.Message);
        }
    }

    private sealed record CloudKktResponse(
        long FiscalDocumentNumber,
        string FiscalStorageNumber,
        string FiscalAttribute,
        string? FiscalSign,
        string QrPayload,
        DateTimeOffset? RegisteredAt);
}
