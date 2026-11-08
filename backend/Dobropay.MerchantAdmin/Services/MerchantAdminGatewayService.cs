using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
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
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add(HttpHeaderNames.InternalKey, options.InternalServiceKey);

        var authFailure = await AuthorizeShopAccessAsync(client, authorizationHeader, shopId, cancellationToken);
        if (authFailure is not null)
        {
            return authFailure;
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

    public Task<IResult> RetryDeliveryAsync(
        string? authorizationHeader,
        Guid shopId,
        Guid deliveryId,
        CancellationToken cancellationToken) =>
        ForwardShopCommandAsync(
            authorizationHeader,
            null,
            shopId,
            options.NotificationsBaseUrl,
            $"{InternalRouteNames.DeliveriesSuffix}/{deliveryId}/{InternalRouteNames.RetrySuffix}",
            Stream.Null,
            cancellationToken);

    public async Task<IResult> ForwardShopCommandAsync(
        string? authorizationHeader,
        string? idempotencyKey,
        Guid shopId,
        string targetBaseUrl,
        string resourcePath,
        Stream requestBody,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add(HttpHeaderNames.InternalKey, options.InternalServiceKey);

        var authFailure = await AuthorizeShopAccessAsync(client, authorizationHeader, shopId, cancellationToken);
        if (authFailure is not null)
        {
            return authFailure;
        }

        using var memoryStream = new MemoryStream();
        await requestBody.CopyToAsync(memoryStream, cancellationToken);
        var bodyBytes = memoryStream.Length > 0
            ? memoryStream.ToArray()
            : Encoding.UTF8.GetBytes(DomainConstants.EmptyJsonObject);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{targetBaseUrl}{InternalRouteNames.ShopByIdPrefix}{shopId}/{resourcePath}")
        {
            Content = new ByteArrayContent(bodyBytes)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(DomainConstants.ApplicationJsonContentType);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            request.Headers.Add(HttpHeaderNames.IdempotencyKey, idempotencyKey);
        }

        using var downstreamResponse = await client.SendAsync(request, cancellationToken);
        var json = await downstreamResponse.Content.ReadAsStringAsync(cancellationToken);

        return Results.Content(
            json,
            DomainConstants.ApplicationJsonContentType,
            statusCode: (int)downstreamResponse.StatusCode);
    }

    private async Task<IResult?> AuthorizeShopAccessAsync(
        HttpClient client,
        string? authorizationHeader,
        Guid shopId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            !authorizationHeader.StartsWith(AuthSchemeNames.BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Results.Unauthorized();
        }

        var token = authorizationHeader[AuthSchemeNames.BearerPrefix.Length..];

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

        return null;
    }
}
