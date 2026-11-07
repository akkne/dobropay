using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.Messaging.Database;
using Dobropay.Receipts.Persistence;

namespace Dobropay.Receipts.Api;

public static class ReceiptsEndpoints
{
    public static void MapReceiptsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(HealthRouteNames.Live, () => Results.Ok(new { status = HealthRouteNames.StatusLive }));

        app.MapGet(HealthRouteNames.Ready, async (IDatabaseHealthChecker healthChecker, CancellationToken ct) =>
            await healthChecker.CanConnectAsync(ct)
                ? Results.Ok(new { status = HealthRouteNames.StatusReady })
                : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

        app.MapGet(InternalRouteNames.ShopReceipts, async (
            HttpContext http,
            Guid shopId,
            IInternalKeyValidator keyValidator,
            IReceiptsRepository repository,
            CancellationToken ct) =>
        {
            if (!keyValidator.IsValid(http.Request.Headers[HttpHeaderNames.InternalKey]))
            {
                return Results.Unauthorized();
            }

            var receipts = await repository.ListReceiptsByShopAsync(shopId, ct);
            return Results.Ok(receipts);
        });
    }
}
