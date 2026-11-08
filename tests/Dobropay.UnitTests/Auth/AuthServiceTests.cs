using Dobropay.Auth.Models;
using Dobropay.Auth.Persistence;
using Dobropay.Auth.Services;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;

namespace Dobropay.UnitTests.Auth;

public sealed class AuthServiceTests
{
    private readonly FakeAuthRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly Sha256HashProvider _hashProvider = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _service = new AuthService(_repository, _unitOfWork, _hashProvider);
    }

    [Fact]
    public async Task RegisterAsync_WithValidCredentials_CreatesAccountAndActiveSession()
    {
        var request = new RegisterRequest("merchant@dobropay.io", "StrongSecretPassword123!");

        var result = await _service.RegisterAsync(request, CancellationToken.None);

        Assert.Equal(RegisterOutcome.Success, result.Outcome);
        Assert.NotNull(result.Response);
        Assert.False(string.IsNullOrWhiteSpace(result.Response.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Response.RefreshToken));
        Assert.Equal(AuthSchemeNames.Bearer, result.Response.TokenType);
        Assert.Equal(28800, result.Response.ExpiresIn);
        Assert.NotEqual(Guid.Empty, result.Response.AccountId);

        var account = Assert.Single(_repository.Accounts);
        Assert.Equal("merchant@dobropay.io", account.Email);
        Assert.False(string.IsNullOrWhiteSpace(account.PasswordSalt));
        Assert.False(string.IsNullOrWhiteSpace(account.PasswordHash));

        var session = Assert.Single(_repository.Sessions);
        Assert.Equal(account.Id, session.AccountId);
        Assert.Equal(_hashProvider.ComputeSha256Hex(result.Response.AccessToken), session.TokenHash);
        Assert.Equal(_hashProvider.ComputeSha256Hex(result.Response.RefreshToken), session.RefreshTokenHash);
        Assert.False(session.IsRevoked);
        Assert.True(session.ExpiresAt > DateTimeOffset.UtcNow);
        Assert.True(session.RefreshExpiresAt > DateTimeOffset.UtcNow);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("notanemail")]
    [InlineData("@nodomain")]
    [InlineData("missingat.com")]
    public async Task RegisterAsync_WithInvalidEmail_ReturnsInvalidEmail(string email)
    {
        var request = new RegisterRequest(email, "StrongSecretPassword123!");

        var result = await _service.RegisterAsync(request, CancellationToken.None);

        Assert.Equal(RegisterOutcome.InvalidEmail, result.Outcome);
        Assert.Null(result.Response);
        Assert.Empty(_repository.Accounts);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("1234567")]
    public async Task RegisterAsync_WithShortPassword_ReturnsInvalidPassword(string password)
    {
        var request = new RegisterRequest("merchant@dobropay.io", password);

        var result = await _service.RegisterAsync(request, CancellationToken.None);

        Assert.Equal(RegisterOutcome.InvalidPassword, result.Outcome);
        Assert.Null(result.Response);
        Assert.Empty(_repository.Accounts);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsAlreadyExists()
    {
        var request = new RegisterRequest("merchant@dobropay.io", "StrongSecretPassword123!");
        var firstResult = await _service.RegisterAsync(request, CancellationToken.None);
        Assert.Equal(RegisterOutcome.Success, firstResult.Outcome);

        var secondResult = await _service.RegisterAsync(new RegisterRequest("MERCHANT@dobropay.io", "AnotherPassword999!"), CancellationToken.None);

        Assert.Equal(RegisterOutcome.AlreadyExists, secondResult.Outcome);
        Assert.Null(secondResult.Response);
        Assert.Single(_repository.Accounts);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponse()
    {
        var register = await _service.RegisterAsync(new RegisterRequest("user@dobropay.io", "CorrectPassword123!"), CancellationToken.None);
        Assert.NotNull(register.Response);

        var loginResponse = await _service.LoginAsync(new LoginRequest("USER@dobropay.io", "CorrectPassword123!"), CancellationToken.None);

        Assert.NotNull(loginResponse);
        Assert.Equal(register.Response.AccountId, loginResponse.AccountId);
        Assert.False(string.IsNullOrWhiteSpace(loginResponse.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(loginResponse.RefreshToken));
        Assert.Equal(AuthSchemeNames.Bearer, loginResponse.TokenType);
        Assert.Equal(2, _repository.Sessions.Count);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsNull()
    {
        await _service.RegisterAsync(new RegisterRequest("user@dobropay.io", "CorrectPassword123!"), CancellationToken.None);

        var loginResponse = await _service.LoginAsync(new LoginRequest("user@dobropay.io", "WrongPassword!"), CancellationToken.None);

        Assert.Null(loginResponse);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ReturnsNull()
    {
        var loginResponse = await _service.LoginAsync(new LoginRequest("unknown@dobropay.io", "CorrectPassword123!"), CancellationToken.None);

        Assert.Null(loginResponse);
    }

    [Fact]
    public async Task RefreshAsync_WithValidRefreshToken_RotatesTokensAndRevokesOldSession()
    {
        var register = await _service.RegisterAsync(new RegisterRequest("user@dobropay.io", "Password123!"), CancellationToken.None);
        var initialAuth = register.Response!;

        var refreshedAuth = await _service.RefreshAsync(new RefreshRequest(initialAuth.RefreshToken), CancellationToken.None);

        Assert.NotNull(refreshedAuth);
        Assert.NotEqual(initialAuth.AccessToken, refreshedAuth.AccessToken);
        Assert.NotEqual(initialAuth.RefreshToken, refreshedAuth.RefreshToken);
        Assert.Equal(initialAuth.AccountId, refreshedAuth.AccountId);

        var oldSession = _repository.Sessions.Single(x => x.TokenHash == _hashProvider.ComputeSha256Hex(initialAuth.AccessToken));
        Assert.True(oldSession.IsRevoked);

        var newSession = _repository.Sessions.Single(x => x.TokenHash == _hashProvider.ComputeSha256Hex(refreshedAuth.AccessToken));
        Assert.False(newSession.IsRevoked);
    }

    [Fact]
    public async Task RefreshAsync_WithRevokedToken_ReturnsNull()
    {
        var register = await _service.RegisterAsync(new RegisterRequest("user@dobropay.io", "Password123!"), CancellationToken.None);
        var initialAuth = register.Response!;

        await _service.LogoutAsync(initialAuth.AccessToken, CancellationToken.None);

        var refreshResponse = await _service.RefreshAsync(new RefreshRequest(initialAuth.RefreshToken), CancellationToken.None);

        Assert.Null(refreshResponse);
    }

    [Fact]
    public async Task RefreshAsync_WithExpiredSession_ReturnsNull()
    {
        var register = await _service.RegisterAsync(new RegisterRequest("user@dobropay.io", "Password123!"), CancellationToken.None);
        var initialAuth = register.Response!;

        var session = _repository.Sessions.Single(x => x.TokenHash == _hashProvider.ComputeSha256Hex(initialAuth.AccessToken));
        session.RefreshExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5);

        var refreshResponse = await _service.RefreshAsync(new RefreshRequest(initialAuth.RefreshToken), CancellationToken.None);

        Assert.Null(refreshResponse);
    }

    [Fact]
    public async Task LogoutAsync_WithAccessToken_RevokesSession()
    {
        var register = await _service.RegisterAsync(new RegisterRequest("user@dobropay.io", "Password123!"), CancellationToken.None);
        var initialAuth = register.Response!;

        var loggedOut = await _service.LogoutAsync(initialAuth.AccessToken, CancellationToken.None);

        Assert.True(loggedOut);
        var session = _repository.Sessions.Single();
        Assert.True(session.IsRevoked);
    }

    [Fact]
    public async Task LogoutAsync_WithRefreshToken_RevokesSession()
    {
        var register = await _service.RegisterAsync(new RegisterRequest("user@dobropay.io", "Password123!"), CancellationToken.None);
        var initialAuth = register.Response!;

        var loggedOut = await _service.LogoutAsync(initialAuth.RefreshToken, CancellationToken.None);

        Assert.True(loggedOut);
        var session = _repository.Sessions.Single();
        Assert.True(session.IsRevoked);
    }

    [Fact]
    public async Task LogoutAsync_WithInvalidToken_ReturnsFalse()
    {
        var loggedOut = await _service.LogoutAsync("nonexistent-token", CancellationToken.None);

        Assert.False(loggedOut);
    }

    [Fact]
    public async Task GetMeAsync_WithValidActiveSession_ReturnsAuthMeResponse()
    {
        var register = await _service.RegisterAsync(new RegisterRequest("owner@dobropay.io", "Password123!"), CancellationToken.None);
        var auth = register.Response!;

        var me = await _service.GetMeAsync(auth.AccessToken, CancellationToken.None);

        Assert.NotNull(me);
        Assert.Equal(auth.AccountId, me.Id);
        Assert.Equal("owner@dobropay.io", me.Email);
    }

    [Fact]
    public async Task GetMeAsync_WithRevokedSession_ReturnsNull()
    {
        var register = await _service.RegisterAsync(new RegisterRequest("owner@dobropay.io", "Password123!"), CancellationToken.None);
        var auth = register.Response!;

        await _service.LogoutAsync(auth.AccessToken, CancellationToken.None);

        var me = await _service.GetMeAsync(auth.AccessToken, CancellationToken.None);

        Assert.Null(me);
    }

    [Fact]
    public async Task GetMeAsync_WithExpiredSession_ReturnsNull()
    {
        var register = await _service.RegisterAsync(new RegisterRequest("owner@dobropay.io", "Password123!"), CancellationToken.None);
        var auth = register.Response!;

        var session = _repository.Sessions.Single();
        session.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-10);

        var me = await _service.GetMeAsync(auth.AccessToken, CancellationToken.None);

        Assert.Null(me);
    }

    [Fact]
    public async Task ResolveSessionAsync_WithValidToken_ReturnsAccountId()
    {
        var register = await _service.RegisterAsync(new RegisterRequest("owner@dobropay.io", "Password123!"), CancellationToken.None);
        var auth = register.Response!;

        var resolved = await _service.ResolveSessionAsync(new ResolveSessionRequest(auth.AccessToken), CancellationToken.None);

        Assert.NotNull(resolved);
        Assert.Equal(auth.AccountId, resolved.AccountId);
    }
}
