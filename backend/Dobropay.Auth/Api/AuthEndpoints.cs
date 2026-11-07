using Dobropay.Auth.Models;
using Dobropay.Auth.Services;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.Messaging.Database;

namespace Dobropay.Auth.Api;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(HealthRouteNames.Live, () => Results.Ok(new { status = HealthRouteNames.StatusLive }));

        app.MapGet(HealthRouteNames.Ready, async (IDatabaseHealthChecker healthChecker, CancellationToken ct) =>
            await healthChecker.CanConnectAsync(ct)
                ? Results.Ok(new { status = HealthRouteNames.StatusReady })
                : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

        app.MapPost(PublicRouteNames.AuthLogin, async (
            LoginRequest request,
            IAuthService authService,
            CancellationToken ct) =>
        {
            var response = await authService.LoginAsync(request, ct);
            return response is null ? Results.Unauthorized() : Results.Ok(response);
        });

        app.MapPost(InternalRouteNames.ResolveSession, async (
            HttpContext httpContext,
            ResolveSessionRequest request,
            IInternalKeyValidator keyValidator,
            IAuthService authService,
            CancellationToken ct) =>
        {
            if (!keyValidator.IsValid(httpContext.Request.Headers[HttpHeaderNames.InternalKey]))
            {
                return Results.Unauthorized();
            }

            var response = await authService.ResolveSessionAsync(request, ct);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });
    }
}
