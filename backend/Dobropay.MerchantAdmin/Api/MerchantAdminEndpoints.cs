using Dobropay.Contracts.Constants;
using Dobropay.MerchantAdmin.Models;
using Dobropay.MerchantAdmin.Services;

namespace Dobropay.MerchantAdmin.Api;

public static class MerchantAdminEndpoints
{
    public static void MapMerchantAdminEndpoints(
        this IEndpointRouteBuilder app,
        MerchantAdminOptions options)
    {
        app.MapGet(HealthRouteNames.Live, () => Results.Ok(new { status = HealthRouteNames.StatusLive }));
        app.MapGet(HealthRouteNames.Ready, () => Results.Ok(new { status = HealthRouteNames.StatusReady }));

        app.MapGet(PublicRouteNames.AdminShopPayments, (
            HttpContext http,
            Guid shopId,
            IMerchantAdminGatewayService gatewayService,
            CancellationToken ct) =>
            gatewayService.ForwardShopResourceAsync(
                http.Request.Headers.Authorization.ToString(),
                shopId,
                options.PaymentsBaseUrl,
                InternalRouteNames.PaymentsSuffix,
                ct));

        app.MapGet(PublicRouteNames.AdminShopPaymentById, (
            HttpContext http,
            Guid shopId,
            Guid id,
            IMerchantAdminGatewayService gatewayService,
            CancellationToken ct) =>
            gatewayService.ForwardShopResourceAsync(
                http.Request.Headers.Authorization.ToString(),
                shopId,
                options.PaymentsBaseUrl,
                $"{InternalRouteNames.PaymentsSuffix}/{id}",
                ct));

        app.MapGet(PublicRouteNames.AdminShopDeliveries, (
            HttpContext http,
            Guid shopId,
            IMerchantAdminGatewayService gatewayService,
            CancellationToken ct) =>
            gatewayService.ForwardShopResourceAsync(
                http.Request.Headers.Authorization.ToString(),
                shopId,
                options.NotificationsBaseUrl,
                InternalRouteNames.DeliveriesSuffix,
                ct));

        app.MapGet(PublicRouteNames.AdminShopReceipts, (
            HttpContext http,
            Guid shopId,
            IMerchantAdminGatewayService gatewayService,
            CancellationToken ct) =>
            gatewayService.ForwardShopResourceAsync(
                http.Request.Headers.Authorization.ToString(),
                shopId,
                options.ReceiptsBaseUrl,
                InternalRouteNames.ReceiptsSuffix,
                ct));
    }
}
