using Dobropay.DeveloperApi.Models;
using Microsoft.EntityFrameworkCore;

namespace Dobropay.DeveloperApi.Persistence;

public sealed class MerchantRepository(MerchantDbContext dbContext) : IMerchantRepository
{
    public async Task<ResolvedShopResponse?> ResolveApiKeyAsync(string keyHash, CancellationToken cancellationToken)
    {
        var credential = await dbContext.ApiCredentials
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.KeyHash == keyHash, cancellationToken);
        if (credential is null)
        {
            return null;
        }

        return await FindShopByIdAsync(credential.ShopId, cancellationToken);
    }

    public async Task<ResolvedShopResponse?> FindShopByIdAsync(Guid shopId, CancellationToken cancellationToken)
    {
        var shop = await dbContext.Shops
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == shopId, cancellationToken);
        return shop is null ? null : new ResolvedShopResponse(shop.Id, shop.Name);
    }

    public async Task<MembershipResponse?> FindMembershipAsync(
        Guid accountId,
        Guid shopId,
        CancellationToken cancellationToken)
    {
        var membership = await dbContext.ShopMemberships
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ShopId == shopId && x.AccountId == accountId, cancellationToken);
        return membership is null ? null : new MembershipResponse(membership.ShopId, membership.Role);
    }
}
