using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.DeveloperApi.Models;
using Dobropay.DeveloperApi.Persistence;
using Dobropay.Messaging.Database;

namespace Dobropay.DeveloperApi.Api;

public static class DeveloperApiEndpoints
{
    public static void MapDeveloperApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(HealthRouteNames.Live, () => Results.Ok(new { status = HealthRouteNames.StatusLive }));

        app.MapGet(HealthRouteNames.Ready, async (IDatabaseHealthChecker healthChecker, CancellationToken ct) =>
            await healthChecker.CanConnectAsync(ct)
                ? Results.Ok(new { status = HealthRouteNames.StatusReady })
                : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

        app.MapPost(InternalRouteNames.ResolveApiKey, async (
            HttpContext httpContext,
            ResolveKeyRequest request,
            IInternalKeyValidator keyValidator,
            IMerchantRepository repository,
            CancellationToken ct) =>
        {
            if (!keyValidator.IsValid(httpContext.Request.Headers[HttpHeaderNames.InternalKey]))
            {
                return Results.Unauthorized();
            }

            if (request.KeyHash.Length != MerchantSchemaConstants.KeyHashLength)
            {
                return Results.BadRequest();
            }

            var resolved = await repository.ResolveApiKeyAsync(request.KeyHash, ct);
            return resolved is null ? Results.NotFound() : Results.Ok(resolved);
        });

        app.MapGet(InternalRouteNames.ShopById, async (
            HttpContext httpContext,
            Guid shopId,
            IInternalKeyValidator keyValidator,
            IMerchantRepository repository,
            CancellationToken ct) =>
        {
            if (!keyValidator.IsValid(httpContext.Request.Headers[HttpHeaderNames.InternalKey]))
            {
                return Results.Unauthorized();
            }

            var shop = await repository.FindShopByIdAsync(shopId, ct);
            return shop is null ? Results.NotFound() : Results.Ok(shop);
        });

        app.MapGet(InternalRouteNames.MembershipByAccountAndShop, async (
            HttpContext httpContext,
            Guid accountId,
            Guid shopId,
            IInternalKeyValidator keyValidator,
            IMerchantRepository repository,
            CancellationToken ct) =>
        {
            if (!keyValidator.IsValid(httpContext.Request.Headers[HttpHeaderNames.InternalKey]))
            {
                return Results.Unauthorized();
            }

            var membership = await repository.FindMembershipAsync(accountId, shopId, ct);
            return membership is null ? Results.NotFound() : Results.Ok(membership);
        });
    }
}
