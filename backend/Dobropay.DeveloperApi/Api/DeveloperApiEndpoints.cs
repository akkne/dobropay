using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.DeveloperApi.Models;
using Dobropay.DeveloperApi.Persistence;
using Dobropay.DeveloperApi.Services;
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

        app.MapPost(InternalRouteNames.ResolveApiKey, HandleResolveApiKeyAsync);
        app.MapGet(InternalRouteNames.ShopById, HandleFindShopByIdAsync);
        app.MapGet(InternalRouteNames.MembershipByAccountAndShop, HandleFindMembershipAsync);
        app.MapPost(InternalRouteNames.ShopOnboardingApprove, HandleApproveOnboardingAsync);

        app.MapGet(PublicRouteNames.DeveloperShops, HandleListShopsAsync);
        app.MapPost(PublicRouteNames.DeveloperShops, HandleCreateShopAsync);
        app.MapGet(PublicRouteNames.DeveloperShopOnboarding, HandleGetOnboardingAsync);
        app.MapPut(PublicRouteNames.DeveloperShopOnboarding, HandleUpdateOnboardingAsync);
        app.MapPost(PublicRouteNames.DeveloperShopOnboardingSubmit, HandleSubmitOnboardingAsync);
        app.MapGet(PublicRouteNames.DeveloperShopKeys, HandleListApiKeysAsync);
        app.MapPost(PublicRouteNames.DeveloperShopKeys, HandleCreateApiKeyAsync);
        app.MapDelete(PublicRouteNames.DeveloperShopKeyDelete, HandleRevokeApiKeyAsync);
    }

    public static async Task<IResult> HandleResolveApiKeyAsync(
        HttpContext httpContext,
        ResolveKeyRequest request,
        IInternalKeyValidator keyValidator,
        IMerchantRepository repository,
        CancellationToken ct)
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
    }

    public static async Task<IResult> HandleFindShopByIdAsync(
        HttpContext httpContext,
        Guid shopId,
        IInternalKeyValidator keyValidator,
        IMerchantRepository repository,
        CancellationToken ct)
    {
        if (!keyValidator.IsValid(httpContext.Request.Headers[HttpHeaderNames.InternalKey]))
        {
            return Results.Unauthorized();
        }

        var shop = await repository.FindShopByIdAsync(shopId, ct);
        return shop is null ? Results.NotFound() : Results.Ok(shop);
    }

    public static async Task<IResult> HandleFindMembershipAsync(
        HttpContext httpContext,
        Guid accountId,
        Guid shopId,
        IInternalKeyValidator keyValidator,
        IMerchantRepository repository,
        CancellationToken ct)
    {
        if (!keyValidator.IsValid(httpContext.Request.Headers[HttpHeaderNames.InternalKey]))
        {
            return Results.Unauthorized();
        }

        var membership = await repository.FindMembershipAsync(accountId, shopId, ct);
        return membership is null ? Results.NotFound() : Results.Ok(membership);
    }

    public static async Task<IResult> HandleApproveOnboardingAsync(
        HttpContext httpContext,
        Guid shopId,
        IInternalKeyValidator keyValidator,
        IDeveloperService developerService,
        CancellationToken ct)
    {
        if (!keyValidator.IsValid(httpContext.Request.Headers[HttpHeaderNames.InternalKey]))
        {
            return Results.Unauthorized();
        }

        var result = await developerService.ApproveOnboardingAsync(shopId, ct);
        return ToHttpResult(result);
    }

    public static async Task<IResult> HandleListShopsAsync(
        HttpContext httpContext,
        IAuthSessionResolver sessionResolver,
        IDeveloperService developerService,
        CancellationToken ct)
    {
        var (accountId, errorResult) = await AuthenticateDeveloperAsync(httpContext, sessionResolver, ct);
        if (errorResult is not null)
        {
            return errorResult;
        }

        var shops = await developerService.ListShopsAsync(accountId!.Value, ct);
        return Results.Ok(shops);
    }

    public static async Task<IResult> HandleCreateShopAsync(
        HttpContext httpContext,
        CreateShopRequest request,
        IAuthSessionResolver sessionResolver,
        IDeveloperService developerService,
        CancellationToken ct)
    {
        var (accountId, errorResult) = await AuthenticateDeveloperAsync(httpContext, sessionResolver, ct);
        if (errorResult is not null)
        {
            return errorResult;
        }

        var result = await developerService.CreateShopAsync(accountId!.Value, request, ct);
        return ToHttpResult(result);
    }

    public static async Task<IResult> HandleGetOnboardingAsync(
        HttpContext httpContext,
        Guid shopId,
        IAuthSessionResolver sessionResolver,
        IDeveloperService developerService,
        CancellationToken ct)
    {
        var (accountId, errorResult) = await AuthenticateDeveloperAsync(httpContext, sessionResolver, ct);
        if (errorResult is not null)
        {
            return errorResult;
        }

        var result = await developerService.GetOnboardingAsync(accountId!.Value, shopId, ct);
        return ToHttpResult(result);
    }

    public static async Task<IResult> HandleUpdateOnboardingAsync(
        HttpContext httpContext,
        Guid shopId,
        UpdateOnboardingRequest request,
        IAuthSessionResolver sessionResolver,
        IDeveloperService developerService,
        CancellationToken ct)
    {
        var (accountId, errorResult) = await AuthenticateDeveloperAsync(httpContext, sessionResolver, ct);
        if (errorResult is not null)
        {
            return errorResult;
        }

        var result = await developerService.UpdateOnboardingAsync(accountId!.Value, shopId, request, ct);
        return ToHttpResult(result);
    }

    public static async Task<IResult> HandleSubmitOnboardingAsync(
        HttpContext httpContext,
        Guid shopId,
        SubmitOnboardingRequest? request,
        IAuthSessionResolver sessionResolver,
        IDeveloperService developerService,
        CancellationToken ct)
    {
        var (accountId, errorResult) = await AuthenticateDeveloperAsync(httpContext, sessionResolver, ct);
        if (errorResult is not null)
        {
            return errorResult;
        }

        var result = await developerService.SubmitOnboardingAsync(accountId!.Value, shopId, request, ct);
        return ToHttpResult(result);
    }

    public static async Task<IResult> HandleListApiKeysAsync(
        HttpContext httpContext,
        Guid shopId,
        IAuthSessionResolver sessionResolver,
        IDeveloperService developerService,
        CancellationToken ct)
    {
        var (accountId, errorResult) = await AuthenticateDeveloperAsync(httpContext, sessionResolver, ct);
        if (errorResult is not null)
        {
            return errorResult;
        }

        var result = await developerService.ListApiKeysAsync(accountId!.Value, shopId, ct);
        return ToHttpResult(result);
    }

    public static async Task<IResult> HandleCreateApiKeyAsync(
        HttpContext httpContext,
        Guid shopId,
        CreateApiKeyRequest request,
        IAuthSessionResolver sessionResolver,
        IDeveloperService developerService,
        CancellationToken ct)
    {
        var (accountId, errorResult) = await AuthenticateDeveloperAsync(httpContext, sessionResolver, ct);
        if (errorResult is not null)
        {
            return errorResult;
        }

        var result = await developerService.CreateApiKeyAsync(accountId!.Value, shopId, request, ct);
        return ToHttpResult(result);
    }

    public static async Task<IResult> HandleRevokeApiKeyAsync(
        HttpContext httpContext,
        Guid shopId,
        Guid keyId,
        IAuthSessionResolver sessionResolver,
        IDeveloperService developerService,
        CancellationToken ct)
    {
        var (accountId, errorResult) = await AuthenticateDeveloperAsync(httpContext, sessionResolver, ct);
        if (errorResult is not null)
        {
            return errorResult;
        }

        var result = await developerService.RevokeApiKeyAsync(accountId!.Value, shopId, keyId, ct);
        return ToHttpResult(result);
    }

    private static async Task<(Guid? AccountId, IResult? ErrorResult)> AuthenticateDeveloperAsync(
        HttpContext httpContext,
        IAuthSessionResolver sessionResolver,
        CancellationToken ct)
    {
        var token = ExtractBearerToken(httpContext);
        if (string.IsNullOrWhiteSpace(token))
        {
            return (null, Results.Unauthorized());
        }

        var accountId = await sessionResolver.ResolveAccountIdAsync(token, ct);
        if (accountId is null)
        {
            return (null, Results.Unauthorized());
        }

        return (accountId, null);
    }

    private static string? ExtractBearerToken(HttpContext httpContext)
    {
        var authorization = httpContext.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorization))
        {
            return null;
        }

        if (authorization.StartsWith(AuthSchemeNames.BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var token = authorization[AuthSchemeNames.BearerPrefix.Length..].Trim();
            return string.IsNullOrEmpty(token) ? null : token;
        }

        return null;
    }

    private static IResult ToHttpResult<T>(DeveloperOperationResult<T> result)
    {
        if (result.IsSuccess)
        {
            return result.StatusCode switch
            {
                StatusCodes.Status201Created => Results.Created(string.Empty, result.Value),
                StatusCodes.Status204NoContent => Results.NoContent(),
                _ => Results.Ok(result.Value)
            };
        }

        return Results.Json(
            result.Error,
            statusCode: result.StatusCode,
            contentType: DomainConstants.ApplicationJsonContentType);
    }
}
