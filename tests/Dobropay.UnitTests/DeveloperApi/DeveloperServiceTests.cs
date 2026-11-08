using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.DeveloperApi.Models;
using Dobropay.DeveloperApi.Persistence;
using Dobropay.DeveloperApi.Services;

namespace Dobropay.UnitTests.DeveloperApi;

public sealed class DeveloperServiceTests
{
    private readonly FakeMerchantRepository _repository = new();
    private readonly IHashProvider _hashProvider = new Sha256HashProvider();
    private readonly DeveloperApiOptions _options = new()
    {
        AuthBaseUrl = "http://auth:8080",
        InternalServiceKey = "test-internal-key",
        SandboxEnabled = true
    };

    private DeveloperService CreateService() => new(_repository, _hashProvider, _options);

    [Fact]
    public async Task CreateShop_Fails_WhenNameIsEmpty()
    {
        var service = CreateService();
        var accountId = Guid.NewGuid();

        var result = await service.CreateShopAsync(accountId, new CreateShopRequest("  "), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CreateShop_Succeeds_AndSetsOwnerDraftAndDefaultTestKey()
    {
        var service = CreateService();
        var accountId = Guid.NewGuid();

        var result = await service.CreateShopAsync(accountId, new CreateShopRequest("Test Shop"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        var response = result.Value!;
        Assert.Equal("Test Shop", response.Name);
        Assert.Equal(DomainConstants.OwnerRole, response.Role);
        Assert.Equal(VerificationStatuses.Draft, response.Onboarding.VerificationStatus);
        Assert.StartsWith(DomainConstants.TestApiKeyPrefix, response.DefaultKey.ApiKey, StringComparison.Ordinal);
        Assert.Equal(DomainConstants.ApiKeyModeTest, response.DefaultKey.Mode);

        Assert.Single(_repository.Shops);
        Assert.Single(_repository.Memberships);
        Assert.Single(_repository.OnboardingProfiles);
        Assert.Single(_repository.Credentials);

        var storedKey = _repository.Credentials[0];
        Assert.NotEqual(response.DefaultKey.ApiKey, storedKey.KeyHash);
        Assert.Equal(_hashProvider.ComputeSha256Hex(response.DefaultKey.ApiKey), storedKey.KeyHash);
    }

    [Fact]
    public async Task ListShops_ReturnsUserShops()
    {
        var service = CreateService();
        var accountId = Guid.NewGuid();

        await service.CreateShopAsync(accountId, new CreateShopRequest("Shop 1"), CancellationToken.None);
        await service.CreateShopAsync(accountId, new CreateShopRequest("Shop 2"), CancellationToken.None);

        var shops = await service.ListShopsAsync(accountId, CancellationToken.None);

        Assert.Equal(2, shops.Count);
        Assert.All(shops, s => Assert.Equal(DomainConstants.OwnerRole, s.Role));
    }

    [Fact]
    public async Task GetOnboarding_Fails_WhenNotMember()
    {
        var service = CreateService();
        var shopId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var result = await service.GetOnboardingAsync(accountId, shopId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.StatusCode);
    }

    [Theory]
    [InlineData(DomainConstants.OwnerRole)]
    [InlineData(DomainConstants.AdminRole)]
    [InlineData(DomainConstants.DeveloperRole)]
    [InlineData(DomainConstants.AnalystRole)]
    public async Task GetOnboarding_Succeeds_ForAllRoles(string role)
    {
        var service = CreateService();
        var shopId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        _repository.Memberships.Add(new ShopMembership { ShopId = shopId, AccountId = accountId, Role = role });
        _repository.OnboardingProfiles.Add(new ShopOnboardingProfile
        {
            ShopId = shopId,
            Inn = "7701234567",
            VerificationStatus = VerificationStatuses.Draft
        });

        var result = await service.GetOnboardingAsync(accountId, shopId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("7701234567", result.Value!.Inn);
    }

    [Theory]
    [InlineData(DomainConstants.DeveloperRole)]
    [InlineData(DomainConstants.AnalystRole)]
    public async Task UpdateOnboarding_Fails_ForDeveloperAndAnalyst(string role)
    {
        var service = CreateService();
        var shopId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        _repository.Memberships.Add(new ShopMembership { ShopId = shopId, AccountId = accountId, Role = role });

        var request = new UpdateOnboardingRequest("7701234567", "1027700123456", "Legal Corp", "044525225", "40702810400000000001", "https://dobropay.io");
        var result = await service.UpdateOnboardingAsync(accountId, shopId, request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task UpdateOnboarding_Succeeds_ForAdminOrOwner_InDraftStatus()
    {
        var service = CreateService();
        var shopId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        _repository.Memberships.Add(new ShopMembership { ShopId = shopId, AccountId = accountId, Role = DomainConstants.AdminRole });
        _repository.OnboardingProfiles.Add(new ShopOnboardingProfile
        {
            ShopId = shopId,
            VerificationStatus = VerificationStatuses.Draft
        });

        var request = new UpdateOnboardingRequest("7701234567", "1027700123456", "Legal Corp", "044525225", "40702810400000000001", "https://dobropay.io");
        var result = await service.UpdateOnboardingAsync(accountId, shopId, request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("7701234567", result.Value!.Inn);
        Assert.Equal("Legal Corp", result.Value.LegalName);
    }

    [Fact]
    public async Task UpdateOnboarding_Fails_WhenApprovedOrUnderReview()
    {
        var service = CreateService();
        var shopId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        _repository.Memberships.Add(new ShopMembership { ShopId = shopId, AccountId = accountId, Role = DomainConstants.OwnerRole });
        _repository.OnboardingProfiles.Add(new ShopOnboardingProfile
        {
            ShopId = shopId,
            VerificationStatus = VerificationStatuses.Approved
        });

        var request = new UpdateOnboardingRequest("7701234567", "1027700123456", "Legal Corp", "044525225", "40702810400000000001", "https://dobropay.io");
        var result = await service.UpdateOnboardingAsync(accountId, shopId, request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(ErrorCodes.InvalidOnboardingProfile, result.Error!.Error);
    }

    [Fact]
    public async Task SubmitOnboarding_Fails_WhenValidationFails()
    {
        var service = CreateService();
        var shopId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        _repository.Memberships.Add(new ShopMembership { ShopId = shopId, AccountId = accountId, Role = DomainConstants.OwnerRole });
        _repository.OnboardingProfiles.Add(new ShopOnboardingProfile
        {
            ShopId = shopId,
            Inn = "123",
            VerificationStatus = VerificationStatuses.Draft
        });

        var result = await service.SubmitOnboardingAsync(accountId, shopId, null, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(ErrorCodes.InvalidOnboardingProfile, result.Error!.Error);
    }

    [Fact]
    public async Task SubmitOnboarding_Succeeds_AndTransitionsToUnderReview()
    {
        var service = CreateService();
        var shopId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        _repository.Memberships.Add(new ShopMembership { ShopId = shopId, AccountId = accountId, Role = DomainConstants.OwnerRole });
        _repository.OnboardingProfiles.Add(new ShopOnboardingProfile
        {
            ShopId = shopId,
            Inn = "7701234567",
            Ogrn = "1027700123456",
            LegalName = "Valid Company LLC",
            BankBic = "044525225",
            BankAccount = "40702810400000000001",
            WebsiteUrl = "https://example.com",
            VerificationStatus = VerificationStatuses.Draft
        });

        var result = await service.SubmitOnboardingAsync(accountId, shopId, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(VerificationStatuses.UnderReview, result.Value!.VerificationStatus);
    }

    [Fact]
    public async Task SubmitOnboarding_WithAutoApprove_TransitionsToApproved()
    {
        var service = CreateService();
        var shopId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        _repository.Memberships.Add(new ShopMembership { ShopId = shopId, AccountId = accountId, Role = DomainConstants.OwnerRole });
        _repository.OnboardingProfiles.Add(new ShopOnboardingProfile
        {
            ShopId = shopId,
            Inn = "7701234567",
            Ogrn = "1027700123456",
            LegalName = "Valid Company LLC",
            BankBic = "044525225",
            BankAccount = "40702810400000000001",
            WebsiteUrl = "https://example.com",
            VerificationStatus = VerificationStatuses.Draft
        });

        var result = await service.SubmitOnboardingAsync(accountId, shopId, new SubmitOnboardingRequest(AutoApprove: true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(VerificationStatuses.Approved, result.Value!.VerificationStatus);
    }

    [Fact]
    public async Task ApproveOnboarding_Internal_SetsStatusApproved()
    {
        var service = CreateService();
        var shopId = Guid.NewGuid();

        _repository.OnboardingProfiles.Add(new ShopOnboardingProfile
        {
            ShopId = shopId,
            VerificationStatus = VerificationStatuses.UnderReview
        });

        var result = await service.ApproveOnboardingAsync(shopId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(VerificationStatuses.Approved, result.Value!.VerificationStatus);
    }

    [Fact]
    public async Task CreateApiKey_Fails_ForAnalystRole()
    {
        var service = CreateService();
        var shopId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        _repository.Memberships.Add(new ShopMembership { ShopId = shopId, AccountId = accountId, Role = DomainConstants.AnalystRole });

        var result = await service.CreateApiKeyAsync(accountId, shopId, new CreateApiKeyRequest("My Key", DomainConstants.ApiKeyModeTest), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task CreateApiKey_Test_Allowed_EvenWhenDraft()
    {
        var service = CreateService();
        var shopId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        _repository.Memberships.Add(new ShopMembership { ShopId = shopId, AccountId = accountId, Role = DomainConstants.DeveloperRole });
        _repository.OnboardingProfiles.Add(new ShopOnboardingProfile
        {
            ShopId = shopId,
            VerificationStatus = VerificationStatuses.Draft
        });

        var result = await service.CreateApiKeyAsync(accountId, shopId, new CreateApiKeyRequest("Dev Test Key", DomainConstants.ApiKeyModeTest), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        Assert.StartsWith(DomainConstants.TestApiKeyPrefix, result.Value!.ApiKey, StringComparison.Ordinal);
        Assert.Equal(DomainConstants.ApiKeyModeTest, result.Value.Mode);
    }

    [Fact]
    public async Task CreateApiKey_Live_Forbidden_WhenNotApproved()
    {
        var service = CreateService();
        var shopId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        _repository.Memberships.Add(new ShopMembership { ShopId = shopId, AccountId = accountId, Role = DomainConstants.DeveloperRole });
        _repository.OnboardingProfiles.Add(new ShopOnboardingProfile
        {
            ShopId = shopId,
            VerificationStatus = VerificationStatuses.UnderReview
        });

        var result = await service.CreateApiKeyAsync(accountId, shopId, new CreateApiKeyRequest("Live Key", DomainConstants.ApiKeyModeLive), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.StatusCode);
        Assert.Equal(ErrorCodes.OnboardingNotApproved, result.Error!.Error);
    }

    [Fact]
    public async Task CreateApiKey_Live_Succeeds_WhenApproved()
    {
        var service = CreateService();
        var shopId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        _repository.Memberships.Add(new ShopMembership { ShopId = shopId, AccountId = accountId, Role = DomainConstants.DeveloperRole });
        _repository.OnboardingProfiles.Add(new ShopOnboardingProfile
        {
            ShopId = shopId,
            VerificationStatus = VerificationStatuses.Approved
        });

        var result = await service.CreateApiKeyAsync(accountId, shopId, new CreateApiKeyRequest("Prod Live Key", DomainConstants.ApiKeyModeLive), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        Assert.StartsWith(DomainConstants.LiveApiKeyPrefix, result.Value!.ApiKey, StringComparison.Ordinal);
        Assert.Equal(DomainConstants.ApiKeyModeLive, result.Value.Mode);

        var credential = _repository.Credentials.Single(x => x.Id == result.Value.Id);
        Assert.Equal(_hashProvider.ComputeSha256Hex(result.Value.ApiKey), credential.KeyHash);
    }

    [Fact]
    public async Task RevokeApiKey_Fails_ForAnalyst()
    {
        var service = CreateService();
        var shopId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var keyId = Guid.NewGuid();

        _repository.Memberships.Add(new ShopMembership { ShopId = shopId, AccountId = accountId, Role = DomainConstants.AnalystRole });

        var result = await service.RevokeApiKeyAsync(accountId, shopId, keyId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task RevokeApiKey_Succeeds_ForDeveloper_AndExcludesFromActiveKeys()
    {
        var service = CreateService();
        var shopId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var keyId = Guid.NewGuid();

        _repository.Memberships.Add(new ShopMembership { ShopId = shopId, AccountId = accountId, Role = DomainConstants.DeveloperRole });
        _repository.Credentials.Add(new ApiCredential
        {
            Id = keyId,
            ShopId = shopId,
            KeyHash = "hash123",
            Name = "Test Key",
            Mode = DomainConstants.ApiKeyModeTest
        });

        var result = await service.RevokeApiKeyAsync(accountId, shopId, keyId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var activeKeys = await service.ListApiKeysAsync(accountId, shopId, CancellationToken.None);
        Assert.Empty(activeKeys.Value!);

        var resolved = await _repository.ResolveApiKeyAsync("hash123", CancellationToken.None);
        Assert.Null(resolved);
    }
}
