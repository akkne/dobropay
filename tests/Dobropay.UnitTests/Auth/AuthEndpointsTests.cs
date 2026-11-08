using System.IO;
using System.Text;
using System.Text.Json;
using Dobropay.Auth.Api;
using Dobropay.Auth.Models;
using Dobropay.Auth.Services;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Dobropay.UnitTests.Auth;

public sealed class AuthEndpointsTests
{
    private readonly FakeAuthRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly Sha256HashProvider _hashProvider = new();
    private readonly IAuthService _authService;
    private readonly IInternalKeyValidator _keyValidator;
    private const string InternalKey = "test-internal-key";

    public AuthEndpointsTests()
    {
        _authService = new AuthService(_repository, _unitOfWork, _hashProvider);
        _keyValidator = new InternalKeyValidator(InternalKey, _hashProvider);
    }

    [Fact]
    public async Task HandleRegisterAsync_Success_ReturnsOk()
    {
        var request = new RegisterRequest("merchant@dobropay.io", "StrongSecretPassword123!");

        var result = await AuthEndpoints.HandleRegisterAsync(request, _authService, CancellationToken.None);

        var okResult = Assert.IsType<Ok<AuthResponse>>(result);
        Assert.NotNull(okResult.Value);
        Assert.False(string.IsNullOrWhiteSpace(okResult.Value.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(okResult.Value.RefreshToken));
    }

    [Fact]
    public async Task HandleRegisterAsync_Duplicate_ReturnsConflict()
    {
        var request = new RegisterRequest("merchant@dobropay.io", "StrongSecretPassword123!");
        await AuthEndpoints.HandleRegisterAsync(request, _authService, CancellationToken.None);

        var result = await AuthEndpoints.HandleRegisterAsync(request, _authService, CancellationToken.None);

        var conflict = Assert.IsType<Conflict<AuthErrorResponse>>(result);
        Assert.Equal(ErrorCodes.EmailAlreadyExists, conflict.Value?.Error);
    }

    [Fact]
    public async Task HandleRegisterAsync_InvalidEmail_ReturnsBadRequest()
    {
        var request = new RegisterRequest("not-an-email", "StrongSecretPassword123!");

        var result = await AuthEndpoints.HandleRegisterAsync(request, _authService, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<AuthErrorResponse>>(result);
        Assert.Equal(ErrorCodes.InvalidEmail, badRequest.Value?.Error);
    }

    [Fact]
    public async Task HandleRegisterAsync_ShortPassword_ReturnsBadRequest()
    {
        var request = new RegisterRequest("merchant@dobropay.io", "short");

        var result = await AuthEndpoints.HandleRegisterAsync(request, _authService, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<AuthErrorResponse>>(result);
        Assert.Equal(ErrorCodes.InvalidPassword, badRequest.Value?.Error);
    }

    [Fact]
    public async Task HandleLoginAsync_Valid_ReturnsOk()
    {
        await AuthEndpoints.HandleRegisterAsync(new RegisterRequest("user@dobropay.io", "StrongSecretPassword123!"), _authService, CancellationToken.None);

        var result = await AuthEndpoints.HandleLoginAsync(new LoginRequest("user@dobropay.io", "StrongSecretPassword123!"), _authService, CancellationToken.None);

        var okResult = Assert.IsType<Ok<AuthResponse>>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task HandleLoginAsync_Invalid_ReturnsUnauthorized()
    {
        var result = await AuthEndpoints.HandleLoginAsync(new LoginRequest("user@dobropay.io", "WrongPassword!"), _authService, CancellationToken.None);

        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task HandleRefreshAsync_Valid_ReturnsOk()
    {
        var reg = await _authService.RegisterAsync(new RegisterRequest("user@dobropay.io", "StrongSecretPassword123!"), CancellationToken.None);
        var refreshToken = reg.Response!.RefreshToken;

        var result = await AuthEndpoints.HandleRefreshAsync(new RefreshRequest(refreshToken), _authService, CancellationToken.None);

        var okResult = Assert.IsType<Ok<AuthResponse>>(result);
        Assert.NotNull(okResult.Value);
        Assert.NotEqual(refreshToken, okResult.Value.RefreshToken);
    }

    [Fact]
    public async Task HandleRefreshAsync_Invalid_ReturnsUnauthorized()
    {
        var result = await AuthEndpoints.HandleRefreshAsync(new RefreshRequest("invalid-refresh-token"), _authService, CancellationToken.None);

        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task HandleLogoutAsync_WithHeader_ReturnsOk()
    {
        var reg = await _authService.RegisterAsync(new RegisterRequest("user@dobropay.io", "StrongSecretPassword123!"), CancellationToken.None);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = $"Bearer {reg.Response!.AccessToken}";

        var result = await AuthEndpoints.HandleLogoutAsync(httpContext, _authService, CancellationToken.None);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task HandleLogoutAsync_WithJsonBody_ReturnsOk()
    {
        var reg = await _authService.RegisterAsync(new RegisterRequest("user@dobropay.io", "StrongSecretPassword123!"), CancellationToken.None);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "application/json";
        var json = JsonSerializer.Serialize(new LogoutRequest(Token: reg.Response!.AccessToken));
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var result = await AuthEndpoints.HandleLogoutAsync(httpContext, _authService, CancellationToken.None);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task HandleLogoutAsync_MissingToken_ReturnsBadRequest()
    {
        var httpContext = new DefaultHttpContext();

        var result = await AuthEndpoints.HandleLogoutAsync(httpContext, _authService, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<AuthErrorResponse>>(result);
        Assert.Equal(ErrorCodes.InvalidToken, badRequest.Value?.Error);
    }

    [Fact]
    public async Task HandleMeAsync_WithValidToken_ReturnsOk()
    {
        var reg = await _authService.RegisterAsync(new RegisterRequest("merchant@dobropay.io", "StrongSecretPassword123!"), CancellationToken.None);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = $"Bearer {reg.Response!.AccessToken}";

        var result = await AuthEndpoints.HandleMeAsync(httpContext, _authService, CancellationToken.None);

        var okResult = Assert.IsType<Ok<AuthMeResponse>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal(reg.Response.AccountId, okResult.Value.Id);
        Assert.Equal("merchant@dobropay.io", okResult.Value.Email);
    }

    [Fact]
    public async Task HandleMeAsync_MissingHeader_ReturnsUnauthorized()
    {
        var httpContext = new DefaultHttpContext();

        var result = await AuthEndpoints.HandleMeAsync(httpContext, _authService, CancellationToken.None);

        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task HandleResolveSessionAsync_InvalidKey_ReturnsUnauthorized()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[HttpHeaderNames.InternalKey] = "invalid";

        var result = await AuthEndpoints.HandleResolveSessionAsync(
            httpContext,
            new ResolveSessionRequest("token"),
            _keyValidator,
            _authService,
            CancellationToken.None);

        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task HandleResolveSessionAsync_ValidKeyAndSession_ReturnsOk()
    {
        var reg = await _authService.RegisterAsync(new RegisterRequest("user@dobropay.io", "StrongSecretPassword123!"), CancellationToken.None);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[HttpHeaderNames.InternalKey] = InternalKey;

        var result = await AuthEndpoints.HandleResolveSessionAsync(
            httpContext,
            new ResolveSessionRequest(reg.Response!.AccessToken),
            _keyValidator,
            _authService,
            CancellationToken.None);

        var okResult = Assert.IsType<Ok<ResolveSessionResponse>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal(reg.Response.AccountId, okResult.Value.AccountId);
    }
}
