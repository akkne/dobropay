using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Receipts;
using Dobropay.Receipts.Models;

namespace Dobropay.Receipts.Services;

public sealed class PaymentReceiptContextClient(
    IHttpClientFactory httpClientFactory,
    ReceiptsOptions options,
    ILogger<PaymentReceiptContextClient> logger) : IPaymentReceiptContextClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PaymentReceiptContextDto?> GetReceiptContextAsync(
        Guid shopId,
        Guid paymentId,
        Guid? refundId,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add(HttpHeaderNames.InternalKey, options.InternalServiceKey);

        var url = $"{options.PaymentsServiceBaseUrl.TrimEnd('/')}/internal/v1/shops/{shopId}/payments/{paymentId}/receipt-context";
        if (refundId.HasValue && refundId.Value != Guid.Empty)
        {
            url += $"?refund_id={refundId.Value}";
        }

        try
        {
            using var response = await client.GetAsync(url, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogWarning(
                    "Receipt context not found for shop {ShopId} payment {PaymentId} refund {RefundId}",
                    shopId,
                    paymentId,
                    refundId);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError(
                    "Failed to fetch receipt context for shop {ShopId} payment {PaymentId}. Status: {Status}, Error: {Error}",
                    shopId,
                    paymentId,
                    response.StatusCode,
                    errorText);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<PaymentReceiptContextDto>(JsonOptions, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Exception occurred while requesting receipt context for shop {ShopId} payment {PaymentId}",
                shopId,
                paymentId);
            return null;
        }
    }
}
