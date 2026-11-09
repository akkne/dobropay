using Dobropay.Contracts.Constants;
using Dobropay.Payments.Services;

namespace Dobropay.Payments.Api;

public sealed class PaymentAvailabilityMiddleware(RequestDelegate next, PaymentsOptions options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!options.SandboxEnabled &&
            (!HttpMethods.IsGet(context.Request.Method) || context.Request.Path == HealthRouteNames.Ready))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }

        await next(context);
    }
}
