namespace Dobropay.MerchantAdmin.Services;

public interface IMerchantAdminGatewayService
{
    Task<IResult> ForwardShopResourceAsync(
        string? authorizationHeader,
        Guid shopId,
        string targetBaseUrl,
        string resourcePath,
        CancellationToken cancellationToken);

    Task<IResult> ForwardShopCommandAsync(
        string? authorizationHeader,
        string? idempotencyKey,
        Guid shopId,
        string targetBaseUrl,
        string resourcePath,
        Stream requestBody,
        CancellationToken cancellationToken);
}
