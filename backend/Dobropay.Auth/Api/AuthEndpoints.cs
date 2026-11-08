using Dobropay.Auth.Models;
using Dobropay.Auth.Services;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.Messaging.Database;

namespace Dobropay.Auth.Api;

public static class AuthEndpoints
{
    private const string BearerPrefix = "Bearer ";

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(HealthRouteNames.Live, () => Results.Ok(new { status = HealthRouteNames.StatusLive }));

        app.MapGet(HealthRouteNames.Ready, async (IDatabaseHealthChecker healthChecker, CancellationToken ct) =>
            await healthChecker.CanConnectAsync(ct)
                ? Results.Ok(new { status = HealthRouteNames.StatusReady })
                : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

        app.MapPost(PublicRouteNames.AuthRegister, HandleRegisterAsync);
        app.MapPost(PublicRouteNames.AuthLogin, HandleLoginAsync);
        app.MapPost(PublicRouteNames.AuthRefresh, HandleRefreshAsync);
        app.MapPost(PublicRouteNames.AuthLogout, HandleLogoutAsync);
        app.MapGet(PublicRouteNames.AuthMe, HandleMeAsync);
        app.MapPost(InternalRouteNames.ResolveSession, HandleResolveSessionAsync);
    }

    public static async Task<IResult> HandleRegisterAsync(
        RegisterRequest request,
        IAuthService authService,
        CancellationToken ct)
    {
        var result = await authService.RegisterAsync(request, ct);
        return result.Outcome switch
        {
            RegisterOutcome.Success => Results.Ok(result.Response),
            RegisterOutcome.AlreadyExists => Results.Conflict(new AuthErrorResponse(ErrorCodes.EmailAlreadyExists)),
            RegisterOutcome.InvalidEmail => Results.BadRequest(new AuthErrorResponse(ErrorCodes.InvalidEmail)),
            RegisterOutcome.InvalidPassword => Results.BadRequest(new AuthErrorResponse(ErrorCodes.InvalidPassword)),
            _ => Results.BadRequest()
        };
    }

    public static async Task<IResult> HandleLoginAsync(
        LoginRequest request,
        IAuthService authService,
        CancellationToken ct)
    {
        var response = await authService.LoginAsync(request, ct);
        return response is null ? Results.Unauthorized() : Results.Ok(response);
    }

    public static async Task<IResult> HandleRefreshAsync(
        RefreshRequest request,
        IAuthService authService,
        CancellationToken ct)
    {
        var response = await authService.RefreshAsync(request, ct);
        return response is null ? Results.Unauthorized() : Results.Ok(response);
    }

    public static async Task<IResult> HandleLogoutAsync(
        HttpContext httpContext,
        IAuthService authService,
        CancellationToken ct)
    {
        var token = ExtractBearerToken(httpContext);
        if (string.IsNullOrWhiteSpace(token) && httpContext.Request.HasJsonContentType())
        {
            try
            {
                var body = await httpContext.Request.ReadFromJsonAsync<LogoutRequest>(cancellationToken: ct);
                token = !string.IsNullOrWhiteSpace(body?.Token) ? body.Token : body?.RefreshToken;
            }
            catch
            {
            }
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return Results.BadRequest(new AuthErrorResponse(ErrorCodes.InvalidToken));
        }

        await authService.LogoutAsync(token, ct);
        return Results.Ok();
    }

    public static async Task<IResult> HandleMeAsync(
        HttpContext httpContext,
        IAuthService authService,
        CancellationToken ct)
    {
        var token = ExtractBearerToken(httpContext);
        if (string.IsNullOrWhiteSpace(token))
        {
            return Results.Unauthorized();
        }

        var response = await authService.GetMeAsync(token, ct);
        return response is null ? Results.Unauthorized() : Results.Ok(response);
    }

    public static async Task<IResult> HandleResolveSessionAsync(
        HttpContext httpContext,
        ResolveSessionRequest request,
        IInternalKeyValidator keyValidator,
        IAuthService authService,
        CancellationToken ct)
    {
        if (!keyValidator.IsValid(httpContext.Request.Headers[HttpHeaderNames.InternalKey]))
        {
            return Results.Unauthorized();
        }

        var response = await authService.ResolveSessionAsync(request, ct);
        return response is null ? Results.NotFound() : Results.Ok(response);
    }

    private static string? ExtractBearerToken(HttpContext httpContext)
    {
        var authorization = httpContext.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorization))
        {
            return null;
        }

        if (authorization.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var token = authorization[BearerPrefix.Length..].Trim();
            return string.IsNullOrEmpty(token) ? null : token;
        }

        return null;
    }
}
