using Dobropay.Contracts.Constants;
using Dobropay.Payments.Api;
using Dobropay.Payments.Services;
using Microsoft.AspNetCore.Http;

namespace Dobropay.UnitTests.Payments;

public sealed class PaymentAvailabilityMiddlewareTests
{
    [Theory]
    [InlineData("POST", "/v1/payments")]
    [InlineData("POST", "/v1/refunds")]
    [InlineData("POST", "/v1/provider/card/3ds-callback")]
    [InlineData("GET", "/health/ready")]
    public async Task Production_BlocksPaymentMutationsAndReadiness(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        var forwarded = false;
        var middleware = new PaymentAvailabilityMiddleware(_ =>
        {
            forwarded = true;
            return Task.CompletedTask;
        }, CreateOptions(false));

        await middleware.InvokeAsync(context);

        Assert.False(forwarded);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }

    [Fact]
    public async Task Production_AllowsReadOnlyPaymentStatus()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = PublicRouteNames.PaymentById.Replace("{id:guid}", Guid.NewGuid().ToString());
        var forwarded = false;
        var middleware = new PaymentAvailabilityMiddleware(_ =>
        {
            forwarded = true;
            return Task.CompletedTask;
        }, CreateOptions(false));

        await middleware.InvokeAsync(context);

        Assert.True(forwarded);
    }

    private static PaymentsOptions CreateOptions(bool sandboxEnabled) => new()
    {
        DeveloperApiBaseUrl = "http://developer-api:8080",
        InternalServiceKey = "key",
        CheckoutSecret = "checkout",
        ProviderSecret = "provider",
        SandboxEnabled = sandboxEnabled,
        PublicBaseUrl = "https://pay.example.com",
        InternalBaseUrl = "http://payments:8080"
    };
}
