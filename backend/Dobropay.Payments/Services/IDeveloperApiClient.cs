namespace Dobropay.Payments.Services;

public interface IDeveloperApiClient
{
    Task<Guid?> ResolveShopIdByApiKeyAsync(string rawApiKey, CancellationToken cancellationToken);
    Task<ResolvedShopDto?> GetShopAsync(Guid shopId, CancellationToken cancellationToken);
}
