using System.Net;
using System.Text;
using Dobropay.Contracts.Constants;
using Dobropay.MerchantAdmin.Models;
using Dobropay.MerchantAdmin.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Dobropay.UnitTests.MerchantAdmin;

public sealed class MerchantAdminRetryDeliveryTests
{
    private static readonly MerchantAdminOptions Options = new()
    {
        AuthBaseUrl = "http://auth:8080",
        DeveloperApiBaseUrl = "http://dev-api:8080",
        PaymentsBaseUrl = "http://payments:8080",
        NotificationsBaseUrl = "http://notifications:8080",
        ReceiptsBaseUrl = "http://receipts:8080",
        InternalServiceKey = "test-internal-key"
    };

    [Fact]
    public async Task RetryDeliveryAsync_ReturnsUnauthorized_WhenAuthHeaderIsMissing()
    {
        var handler = new TestHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new HttpClient(handler);
        var factory = new TestHttpClientFactory(client);
        var service = new MerchantAdminGatewayService(factory, Options);

        var result = await service.RetryDeliveryAsync(
            authorizationHeader: null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task RetryDeliveryAsync_ReturnsUnauthorized_WhenSessionInvalid()
    {
        var handler = new TestHttpMessageHandler((request, _) =>
        {
            if (request.RequestUri?.AbsolutePath == InternalRouteNames.ResolveSession)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var client = new HttpClient(handler);
        var factory = new TestHttpClientFactory(client);
        var service = new MerchantAdminGatewayService(factory, Options);

        var result = await service.RetryDeliveryAsync(
            "Bearer invalid-token",
            Guid.NewGuid(),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task RetryDeliveryAsync_ForwardsRetryToNotifications_WhenAuthorized()
    {
        var shopId = Guid.NewGuid();
        var deliveryId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var forwarded = false;

        var handler = new TestHttpMessageHandler((request, _) =>
        {
            var uri = request.RequestUri?.ToString() ?? string.Empty;

            if (uri.Contains(InternalRouteNames.ResolveSession))
            {
                var json = $"{{\"AccountId\":\"{accountId}\",\"Email\":\"admin@test.com\"}}";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            }

            if (uri.Contains(InternalRouteNames.MembershipPrefix))
            {
                var json = $"{{\"ShopId\":\"{shopId}\",\"Role\":\"admin\"}}";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            }

            if (uri.Contains($"/internal/v1/shops/{shopId}/deliveries/{deliveryId}/retry"))
            {
                forwarded = true;
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal(Options.InternalServiceKey, request.Headers.GetValues(HttpHeaderNames.InternalKey).FirstOrDefault());

                var json = $"{{\"retried\":true,\"delivery_id\":\"{deliveryId}\"}}";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var client = new HttpClient(handler);
        var factory = new TestHttpClientFactory(client);
        var service = new MerchantAdminGatewayService(factory, Options);

        var result = await service.RetryDeliveryAsync(
            "Bearer valid-token",
            shopId,
            deliveryId,
            CancellationToken.None);

        Assert.True(forwarded);
        var contentResult = Assert.IsType<ContentHttpResult>(result);
        Assert.Equal(200, contentResult.StatusCode);
        Assert.Contains(deliveryId.ToString(), contentResult.ResponseContent);
    }
}
