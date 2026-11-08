using Dobropay.Payments.Services;

namespace Dobropay.UnitTests;

public sealed class FakeDeveloperApiClient : IDeveloperApiClient
{
    public Guid? ShopId { get; set; } = Guid.NewGuid();

    public Task<Guid?> ResolveShopIdByApiKeyAsync(string rawApiKey, CancellationToken cancellationToken) =>
        Task.FromResult(ShopId);

    public Task<ResolvedShopDto?> GetShopAsync(Guid shopId, CancellationToken cancellationToken) =>
        Task.FromResult<ResolvedShopDto?>(new ResolvedShopDto(shopId, "Test Shop"));
}
