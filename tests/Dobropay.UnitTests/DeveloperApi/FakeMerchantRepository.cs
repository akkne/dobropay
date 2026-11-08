using Dobropay.Contracts.Constants;
using Dobropay.DeveloperApi.Models;
using Dobropay.DeveloperApi.Persistence;

namespace Dobropay.UnitTests.DeveloperApi;

public sealed class FakeMerchantRepository : IMerchantRepository
{
    public List<Shop> Shops { get; } = [];
    public List<ShopMembership> Memberships { get; } = [];
    public List<ShopOnboardingProfile> OnboardingProfiles { get; } = [];
    public List<ApiCredential> Credentials { get; } = [];

    public Task<ResolvedShopResponse?> ResolveApiKeyAsync(string keyHash, CancellationToken cancellationToken)
    {
        var credential = Credentials.SingleOrDefault(x => x.KeyHash == keyHash && x.RevokedAt == null);
        if (credential is null)
        {
            return Task.FromResult<ResolvedShopResponse?>(null);
        }

        return FindShopByIdAsync(credential.ShopId, cancellationToken);
    }

    public Task<ResolvedShopResponse?> FindShopByIdAsync(Guid shopId, CancellationToken cancellationToken)
    {
        var shop = Shops.SingleOrDefault(x => x.Id == shopId);
        return Task.FromResult(shop is null ? null : new ResolvedShopResponse(shop.Id, shop.Name));
    }

    public Task<MembershipResponse?> FindMembershipAsync(Guid accountId, Guid shopId, CancellationToken cancellationToken)
    {
        var membership = Memberships.SingleOrDefault(x => x.AccountId == accountId && x.ShopId == shopId);
        return Task.FromResult(membership is null ? null : new MembershipResponse(membership.ShopId, membership.Role));
    }

    public Task<IReadOnlyList<ShopListItemResponse>> FindShopsForAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var list = new List<ShopListItemResponse>();
        var userMemberships = Memberships.Where(x => x.AccountId == accountId).ToList();

        foreach (var membership in userMemberships)
        {
            var shop = Shops.SingleOrDefault(x => x.Id == membership.ShopId);
            if (shop is null)
            {
                continue;
            }

            var profile = OnboardingProfiles.SingleOrDefault(x => x.ShopId == shop.Id);
            var status = profile?.VerificationStatus ?? VerificationStatuses.Draft;

            list.Add(new ShopListItemResponse(shop.Id, shop.Name, membership.Role, status));
        }

        return Task.FromResult<IReadOnlyList<ShopListItemResponse>>(list);
    }

    public Task<Shop?> GetShopAsync(Guid shopId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Shops.SingleOrDefault(x => x.Id == shopId));
    }

    public Task<ShopMembership?> GetMembershipAsync(Guid shopId, Guid accountId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Memberships.SingleOrDefault(x => x.ShopId == shopId && x.AccountId == accountId));
    }

    public Task CreateShopWithDefaultsAsync(
        Shop shop,
        ShopMembership membership,
        ShopOnboardingProfile profile,
        ApiCredential defaultKey,
        CancellationToken cancellationToken)
    {
        Shops.Add(shop);
        Memberships.Add(membership);
        OnboardingProfiles.Add(profile);
        Credentials.Add(defaultKey);
        return Task.CompletedTask;
    }

    public Task<ShopOnboardingProfile?> GetOnboardingProfileAsync(Guid shopId, CancellationToken cancellationToken)
    {
        return Task.FromResult(OnboardingProfiles.SingleOrDefault(x => x.ShopId == shopId));
    }

    public Task SaveOnboardingProfileAsync(ShopOnboardingProfile profile, CancellationToken cancellationToken)
    {
        var existing = OnboardingProfiles.SingleOrDefault(x => x.ShopId == profile.ShopId);
        if (existing is null)
        {
            OnboardingProfiles.Add(profile);
        }

        return Task.CompletedTask;
    }

    public Task AddApiKeyAsync(ApiCredential credential, CancellationToken cancellationToken)
    {
        Credentials.Add(credential);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ApiCredential>> GetActiveApiKeysAsync(Guid shopId, CancellationToken cancellationToken)
    {
        var keys = Credentials
            .Where(x => x.ShopId == shopId && x.RevokedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<ApiCredential>>(keys);
    }

    public Task<ApiCredential?> GetActiveApiKeyByIdAsync(Guid shopId, Guid keyId, CancellationToken cancellationToken)
    {
        var key = Credentials.SingleOrDefault(x => x.ShopId == shopId && x.Id == keyId && x.RevokedAt == null);
        return Task.FromResult(key);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
