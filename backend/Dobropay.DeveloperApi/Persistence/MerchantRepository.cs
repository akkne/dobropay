using Dobropay.Contracts.Constants;
using Dobropay.DeveloperApi.Models;
using Microsoft.EntityFrameworkCore;

namespace Dobropay.DeveloperApi.Persistence;

public sealed class MerchantRepository(MerchantDbContext dbContext) : IMerchantRepository
{
    public async Task<ResolvedShopResponse?> ResolveApiKeyAsync(string keyHash, CancellationToken cancellationToken)
    {
        var credential = await dbContext.ApiCredentials
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.KeyHash == keyHash && x.RevokedAt == null, cancellationToken);
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

    public async Task<IReadOnlyList<ShopListItemResponse>> FindShopsForAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var query = from m in dbContext.ShopMemberships.AsNoTracking()
                    where m.AccountId == accountId
                    join s in dbContext.Shops.AsNoTracking() on m.ShopId equals s.Id
                    join p in dbContext.ShopOnboardingProfiles.AsNoTracking() on s.Id equals p.ShopId into profiles
                    from p in profiles.DefaultIfEmpty()
                    orderby s.Name
                    select new ShopListItemResponse(
                        s.Id,
                        s.Name,
                        m.Role,
                        p != null ? p.VerificationStatus : VerificationStatuses.Draft);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Shop?> GetShopAsync(Guid shopId, CancellationToken cancellationToken)
    {
        return await dbContext.Shops
            .SingleOrDefaultAsync(x => x.Id == shopId, cancellationToken);
    }

    public async Task<ShopMembership?> GetMembershipAsync(Guid shopId, Guid accountId, CancellationToken cancellationToken)
    {
        return await dbContext.ShopMemberships
            .SingleOrDefaultAsync(x => x.ShopId == shopId && x.AccountId == accountId, cancellationToken);
    }

    public async Task CreateShopWithDefaultsAsync(
        Shop shop,
        ShopMembership membership,
        ShopOnboardingProfile profile,
        ApiCredential? defaultKey,
        CancellationToken cancellationToken)
    {
        await dbContext.Shops.AddAsync(shop, cancellationToken);
        await dbContext.ShopMemberships.AddAsync(membership, cancellationToken);
        await dbContext.ShopOnboardingProfiles.AddAsync(profile, cancellationToken);
        if (defaultKey is not null)
        {
            await dbContext.ApiCredentials.AddAsync(defaultKey, cancellationToken);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ShopOnboardingProfile?> GetOnboardingProfileAsync(Guid shopId, CancellationToken cancellationToken)
    {
        return await dbContext.ShopOnboardingProfiles
            .SingleOrDefaultAsync(x => x.ShopId == shopId, cancellationToken);
    }

    public async Task SaveOnboardingProfileAsync(ShopOnboardingProfile profile, CancellationToken cancellationToken)
    {
        var existing = await dbContext.ShopOnboardingProfiles
            .SingleOrDefaultAsync(x => x.ShopId == profile.ShopId, cancellationToken);
        if (existing is null)
        {
            await dbContext.ShopOnboardingProfiles.AddAsync(profile, cancellationToken);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddApiKeyAsync(ApiCredential credential, CancellationToken cancellationToken)
    {
        await dbContext.ApiCredentials.AddAsync(credential, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ApiCredential>> GetActiveApiKeysAsync(Guid shopId, CancellationToken cancellationToken)
    {
        return await dbContext.ApiCredentials
            .AsNoTracking()
            .Where(x => x.ShopId == shopId && x.RevokedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ApiCredential?> GetActiveApiKeyByIdAsync(Guid shopId, Guid keyId, CancellationToken cancellationToken)
    {
        return await dbContext.ApiCredentials
            .SingleOrDefaultAsync(x => x.ShopId == shopId && x.Id == keyId && x.RevokedAt == null, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
