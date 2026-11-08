using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.DeveloperApi.Api;
using Dobropay.DeveloperApi.Models;
using Dobropay.DeveloperApi.Persistence;
using Dobropay.DeveloperApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Dobropay.UnitTests.DeveloperApi;

public sealed class DeveloperApiEndpointsTests
{
    private readonly FakeMerchantRepository _repository = new();
    private readonly FakeAuthSessionResolver _sessionResolver = new();
    private readonly IHashProvider _hashProvider = new Sha256HashProvider();
    private readonly IInternalKeyValidator _keyValidator;
    private readonly IDeveloperService _developerService;
    private const string InternalKey = "test-internal-key";

    public DeveloperApiEndpointsTests()
    {
        _keyValidator = new InternalKeyValidator(InternalKey, _hashProvider);
        var options = new DeveloperApiOptions
        {
            AuthBaseUrl = "http://auth:8080",
            InternalServiceKey = InternalKey,
            SandboxEnabled = true
        };
        _developerService = new DeveloperService(_repository, _hashProvider, options);
    }

    [Fact]
    public async Task HandleCreateShop_MissingBearer_ReturnsUnauthorized()
    {
        var context = new DefaultHttpContext();
        var request = new CreateShopRequest("Shop");

        var result = await DeveloperApiEndpoints.HandleCreateShopAsync(
            context,
            request,
            _sessionResolver,
            _developerService,
            CancellationToken.None);

        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task HandleCreateShop_InvalidBearerToken_ReturnsUnauthorized()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer invalid-token";
        var request = new CreateShopRequest("Shop");

        var result = await DeveloperApiEndpoints.HandleCreateShopAsync(
            context,
            request,
            _sessionResolver,
            _developerService,
            CancellationToken.None);

        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task HandleCreateShop_ValidBearerToken_ReturnsCreated()
    {
        var accountId = Guid.NewGuid();
        const string token = "valid-token-123";
        _sessionResolver.SetSession(token, accountId);

        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = $"Bearer {token}";
        var request = new CreateShopRequest("Super Store");

        var result = await DeveloperApiEndpoints.HandleCreateShopAsync(
            context,
            request,
            _sessionResolver,
            _developerService,
            CancellationToken.None);

        var created = Assert.IsType<Created<CreateShopResponse>>(result);
        Assert.NotNull(created.Value);
        Assert.Equal("Super Store", created.Value.Name);
    }

    [Fact]
    public async Task HandleApproveOnboarding_InvalidInternalKey_ReturnsUnauthorized()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[HttpHeaderNames.InternalKey] = "wrong-key";

        var result = await DeveloperApiEndpoints.HandleApproveOnboardingAsync(
            context,
            Guid.NewGuid(),
            _keyValidator,
            _developerService,
            CancellationToken.None);

        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task HandleApproveOnboarding_ValidInternalKey_ReturnsOk()
    {
        var shopId = Guid.NewGuid();
        _repository.OnboardingProfiles.Add(new ShopOnboardingProfile
        {
            ShopId = shopId,
            VerificationStatus = VerificationStatuses.UnderReview
        });

        var context = new DefaultHttpContext();
        context.Request.Headers[HttpHeaderNames.InternalKey] = InternalKey;

        var result = await DeveloperApiEndpoints.HandleApproveOnboardingAsync(
            context,
            shopId,
            _keyValidator,
            _developerService,
            CancellationToken.None);

        var ok = Assert.IsType<Ok<ShopOnboardingDto>>(result);
        Assert.Equal(VerificationStatuses.Approved, ok.Value!.VerificationStatus);
    }

    [Fact]
    public async Task HandleRevokeApiKey_ValidDeveloper_ReturnsOk()
    {
        var accountId = Guid.NewGuid();
        var shopId = Guid.NewGuid();
        var keyId = Guid.NewGuid();
        const string token = "valid-token-revoke";

        _sessionResolver.SetSession(token, accountId);
        _repository.Memberships.Add(new ShopMembership
        {
            AccountId = accountId,
            ShopId = shopId,
            Role = DomainConstants.DeveloperRole
        });
        _repository.Credentials.Add(new ApiCredential
        {
            Id = keyId,
            ShopId = shopId,
            KeyHash = "somehash",
            Name = "Dev Key",
            Mode = DomainConstants.ApiKeyModeTest
        });

        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = $"Bearer {token}";

        var result = await DeveloperApiEndpoints.HandleRevokeApiKeyAsync(
            context,
            shopId,
            keyId,
            _sessionResolver,
            _developerService,
            CancellationToken.None);

        var ok = Assert.IsType<Ok<bool>>(result);
        Assert.True(ok.Value);
    }
}
