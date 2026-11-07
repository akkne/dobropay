using System.Net.Http.Json;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;

namespace Dobropay.Payments.Services;

public sealed class DeveloperApiClient(
    HttpClient httpClient,
    PaymentsOptions options,
    IHashProvider hashProvider) : IDeveloperApiClient
{
    private const int MinApiKeyLength = 8;

    public async Task<Guid?> ResolveShopIdByApiKeyAsync(string rawApiKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawApiKey) || rawApiKey.Length < MinApiKeyLength)
        {
            return null;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            options.DeveloperApiBaseUrl + InternalRouteNames.ResolveApiKey);
        request.Headers.Add(HttpHeaderNames.InternalKey, options.InternalServiceKey);
        request.Content = JsonContent.Create(new { KeyHash = hashProvider.ComputeSha256Hex(rawApiKey) });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var shop = await response.Content.ReadFromJsonAsync<ResolvedShopDto>(cancellationToken: cancellationToken);
        return shop?.ShopId;
    }

    public async Task<ResolvedShopDto?> GetShopAsync(Guid shopId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            options.DeveloperApiBaseUrl + InternalRouteNames.ShopByIdPrefix + shopId);
        request.Headers.Add(HttpHeaderNames.InternalKey, options.InternalServiceKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ResolvedShopDto>(cancellationToken: cancellationToken);
    }
}
