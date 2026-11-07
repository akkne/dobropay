namespace Dobropay.MerchantAdmin.Services;

public interface IMerchantAdminGatewayService
{
    Task<IResult> ForwardShopResourceAsync(
        string? authorizationHeader,
        Guid shopId,
        string targetBaseUrl,
        string resourcePath,
        CancellationToken cancellationToken);
}
