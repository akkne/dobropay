using Dobropay.DeveloperApi.Models;

namespace Dobropay.DeveloperApi.Persistence;

public interface IMerchantRepository
{
    Task<ResolvedShopResponse?> ResolveApiKeyAsync(string keyHash, CancellationToken cancellationToken);
    Task<ResolvedShopResponse?> FindShopByIdAsync(Guid shopId, CancellationToken cancellationToken);
    Task<MembershipResponse?> FindMembershipAsync(Guid accountId, Guid shopId, CancellationToken cancellationToken);
}
