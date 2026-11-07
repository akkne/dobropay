using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.Payments.Domain;
using Dobropay.Payments.Persistence;
using Dobropay.Payments.Services;

namespace Dobropay.Payments.Api;

public static class RefundsEndpoints
{
    private const int MaxKeyLength = 200;
    private const int DefaultRefundsListLimit = 100;

    public static void MapRefundsEndpoints(this WebApplication app)
    {
        app.MapPost(PublicRouteNames.Refunds, async (
            HttpContext http,
            CreateRefundRequest body,
            IDeveloperApiClient developerApiClient,
            IRefundService refundService,
            CancellationToken ct) =>
        {
            var shopId = await developerApiClient.ResolveShopIdByApiKeyAsync(
                http.Request.Headers[HttpHeaderNames.ApiKey].ToString(),
                ct);
            if (shopId is null)
            {
                return Results.Unauthorized();
            }

            var idempotencyKey = http.Request.Headers[HttpHeaderNames.IdempotencyKey].ToString();
            if (idempotencyKey.Length is < 1 or > MaxKeyLength)
            {
                return Results.BadRequest(new { error = ErrorCodes.IdempotencyKeyRequired });
            }

            var result = await refundService.CreateRefundAsync(
                shopId.Value,
                idempotencyKey,
                body,
                http.TraceIdentifier,
                ct);
            return MapRefundResult(result);
        })
        .WithTags("Refunds")
        .WithSummary("Create a full or partial refund")
        .Produces<RefundView>(StatusCodes.Status201Created)
        .Produces<RefundView>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        app.MapGet(PublicRouteNames.RefundById, async (
            HttpContext http,
            Guid id,
            IDeveloperApiClient developerApiClient,
            IRefundRepository refundRepository,
            CancellationToken ct) =>
        {
            var shopId = await developerApiClient.ResolveShopIdByApiKeyAsync(
                http.Request.Headers[HttpHeaderNames.ApiKey].ToString(),
                ct);
            if (shopId is null)
            {
                return Results.Unauthorized();
            }

            var refund = await refundRepository.FindByIdAsync(shopId.Value, id, ct);
            return refund is null
                ? Results.NotFound(new { error = ErrorCodes.RefundNotFound })
                : Results.Ok(BuildRefundView(refund));
        })
        .WithTags("Refunds")
        .WithSummary("Get own refund")
        .Produces<RefundView>()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound);

        app.MapGet(PublicRouteNames.PaymentRefunds, async (
            HttpContext http,
            Guid id,
            IDeveloperApiClient developerApiClient,
            IPaymentsRepository paymentsRepository,
            IRefundRepository refundRepository,
            CancellationToken ct) =>
        {
            var shopId = await developerApiClient.ResolveShopIdByApiKeyAsync(
                http.Request.Headers[HttpHeaderNames.ApiKey].ToString(),
                ct);
            if (shopId is null)
            {
                return Results.Unauthorized();
            }

            var payment = await paymentsRepository.FindPaymentByIdAsync(shopId.Value, id, ct);
            if (payment is null)
            {
                return Results.NotFound(new { error = ErrorCodes.PaymentNotFound });
            }

            var refunds = await refundRepository.ListByPaymentAsync(shopId.Value, id, ct);
            return Results.Ok(refunds);
        })
        .WithTags("Refunds")
        .WithSummary("List refunds for a payment")
        .Produces<IReadOnlyList<RefundView>>()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound);

        app.MapGet(InternalRouteNames.ShopRefunds, async (
            HttpContext http,
            Guid shopId,
            IInternalKeyValidator keyValidator,
            IRefundRepository refundRepository,
            CancellationToken ct) =>
        {
            if (!keyValidator.IsValid(http.Request.Headers[HttpHeaderNames.InternalKey]))
            {
                return Results.Unauthorized();
            }

            var refunds = await refundRepository.ListByShopAsync(shopId, DefaultRefundsListLimit, ct);
            return Results.Ok(refunds);
        });

        app.MapGet(InternalRouteNames.ShopPaymentRefunds, async (
            HttpContext http,
            Guid shopId,
            Guid id,
            IInternalKeyValidator keyValidator,
            IRefundRepository refundRepository,
            CancellationToken ct) =>
        {
            if (!keyValidator.IsValid(http.Request.Headers[HttpHeaderNames.InternalKey]))
            {
                return Results.Unauthorized();
            }

            var refunds = await refundRepository.ListByPaymentAsync(shopId, id, ct);
            return Results.Ok(refunds);
        });

        app.MapPost(InternalRouteNames.ShopPaymentRefunds, async (
            HttpContext http,
            Guid shopId,
            Guid id,
            CreatePaymentRefundRequest body,
            IInternalKeyValidator keyValidator,
            IRefundService refundService,
            CancellationToken ct) =>
        {
            if (!keyValidator.IsValid(http.Request.Headers[HttpHeaderNames.InternalKey]))
            {
                return Results.Unauthorized();
            }

            var idempotencyKey = ResolveInternalIdempotencyKey(http.Request);
            if (idempotencyKey is null)
            {
                return Results.BadRequest(new { error = ErrorCodes.IdempotencyKeyRequired });
            }

            var result = await refundService.CreateRefundAsync(
                shopId,
                idempotencyKey,
                new CreateRefundRequest(id, body.Amount, body.Currency, body.Reason),
                http.TraceIdentifier,
                ct);
            return MapRefundResult(result);
        });
    }

    private static RefundView BuildRefundView(Refund refund) =>
        new(
            refund.Id,
            refund.PaymentId,
            refund.Status,
            refund.Amount,
            refund.Currency,
            refund.Reason,
            refund.ExternalRefundId,
            refund.CreatedAt,
            refund.CompletedAt);

    private static string? ResolveInternalIdempotencyKey(HttpRequest request)
    {
        var key = request.Headers[HttpHeaderNames.IdempotencyKey].ToString();
        if (string.IsNullOrWhiteSpace(key))
        {
            return Guid.NewGuid().ToString("N");
        }

        return key.Length > MaxKeyLength ? null : key;
    }

    private static IResult MapRefundResult(RefundOperationResult result) =>
        result.Outcome switch
        {
            RefundOperationOutcome.Success when result.Created && result.Refund is not null =>
                Results.Created($"{PublicRouteNames.Refunds}/{result.Refund.Id}", result.Refund),
            RefundOperationOutcome.Success =>
                Results.Ok(result.Refund),
            RefundOperationOutcome.PaymentNotFound =>
                Results.NotFound(new { error = result.ErrorCode ?? ErrorCodes.PaymentNotFound }),
            RefundOperationOutcome.RefundNotFound =>
                Results.NotFound(new { error = result.ErrorCode ?? ErrorCodes.RefundNotFound }),
            RefundOperationOutcome.IdempotencyConflict =>
                Results.Conflict(new { error = result.ErrorCode ?? ErrorCodes.IdempotencyConflict }),
            RefundOperationOutcome.InvalidState =>
                Results.Conflict(new { error = result.ErrorCode ?? ErrorCodes.InvalidRefund }),
            RefundOperationOutcome.AmountExceeded =>
                Results.Conflict(new { error = result.ErrorCode ?? ErrorCodes.RefundAmountExceeded }),
            RefundOperationOutcome.InvalidAmount =>
                Results.BadRequest(new { error = result.ErrorCode ?? ErrorCodes.InvalidRefund }),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
        };
}
