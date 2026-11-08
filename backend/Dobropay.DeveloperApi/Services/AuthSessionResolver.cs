using System.Net.Http.Json;
using Dobropay.Contracts.Constants;
using Dobropay.DeveloperApi.Models;

namespace Dobropay.DeveloperApi.Services;

public sealed class AuthSessionResolver(
    IHttpClientFactory httpClientFactory,
    DeveloperApiOptions options) : IAuthSessionResolver
{
    public async Task<Guid?> ResolveAccountIdAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add(HttpHeaderNames.InternalKey, options.InternalServiceKey);

        var response = await client.PostAsJsonAsync(
            $"{options.AuthBaseUrl}{InternalRouteNames.ResolveSession}",
            new { Token = token },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<AuthSessionResolveDto>(cancellationToken: cancellationToken);
        return result?.AccountId;
    }
}
