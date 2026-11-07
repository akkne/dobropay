using System.Net.Http.Json;
using Dobropay.Contracts.Constants;
using Dobropay.MerchantAdmin.Models;

namespace Dobropay.MerchantAdmin.Services;

public sealed class MerchantAdminGatewayService(
    IHttpClientFactory httpClientFactory,
    MerchantAdminOptions options) : IMerchantAdminGatewayService
{
    public async Task<IResult> ForwardShopResourceAsync(
        string? authorizationHeader,
        Guid shopId,
        string targetBaseUrl,
        string resourcePath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            !authorizationHeader.StartsWith(AuthSchemeNames.BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Results.Unauthorized();
        }

        var token = authorizationHeader[AuthSchemeNames.BearerPrefix.Length..];
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add(HttpHeaderNames.InternalKey, options.InternalServiceKey);

        using var sessionResponse = await client.PostAsJsonAsync(
            options.AuthBaseUrl + InternalRouteNames.ResolveSession,
            new { Token = token },
            cancellationToken);
        if (!sessionResponse.IsSuccessStatusCode)
        {
            return Results.Unauthorized();
        }

        var identity = await sessionResponse.Content.ReadFromJsonAsync<ResolvedSessionIdentity>(cancellationToken: cancellationToken);
        if (identity is null)
        {
            return Results.Unauthorized();
        }

        using var membershipResponse = await client.GetAsync(
            $"{options.DeveloperApiBaseUrl}{InternalRouteNames.MembershipPrefix}{identity.AccountId}/{shopId}",
            cancellationToken);
        if (!membershipResponse.IsSuccessStatusCode)
        {
            return Results.NotFound();
        }

        using var downstreamResponse = await client.GetAsync(
            $"{targetBaseUrl}{InternalRouteNames.ShopByIdPrefix}{shopId}/{resourcePath}",
            cancellationToken);
        var json = await downstreamResponse.Content.ReadAsStringAsync(cancellationToken);

        return Results.Content(
            json,
            DomainConstants.ApplicationJsonContentType,
            statusCode: (int)downstreamResponse.StatusCode);
    }
}
