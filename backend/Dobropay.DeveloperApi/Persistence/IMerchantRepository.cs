using Dobropay.DeveloperApi.Models;

namespace Dobropay.DeveloperApi.Persistence;

public interface IMerchantRepository
{
    Task<ResolvedShopResponse?> ResolveApiKeyAsync(string keyHash, CancellationToken cancellationToken);
    Task<ResolvedShopResponse?> FindShopByIdAsync(Guid shopId, CancellationToken cancellationToken);
    Task<MembershipResponse?> FindMembershipAsync(Guid accountId, Guid shopId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ShopListItemResponse>> FindShopsForAccountAsync(Guid accountId, CancellationToken cancellationToken);
    Task<Shop?> GetShopAsync(Guid shopId, CancellationToken cancellationToken);
    Task<ShopMembership?> GetMembershipAsync(Guid shopId, Guid accountId, CancellationToken cancellationToken);
    Task CreateShopWithDefaultsAsync(
        Shop shop,
        ShopMembership membership,
        ShopOnboardingProfile profile,
        ApiCredential? defaultKey,
        CancellationToken cancellationToken);
    Task<ShopOnboardingProfile?> GetOnboardingProfileAsync(Guid shopId, CancellationToken cancellationToken);
    Task SaveOnboardingProfileAsync(ShopOnboardingProfile profile, CancellationToken cancellationToken);
    Task AddApiKeyAsync(ApiCredential credential, CancellationToken cancellationToken);
    Task<IReadOnlyList<ApiCredential>> GetActiveApiKeysAsync(Guid shopId, CancellationToken cancellationToken);
    Task<ApiCredential?> GetActiveApiKeyByIdAsync(Guid shopId, Guid keyId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
