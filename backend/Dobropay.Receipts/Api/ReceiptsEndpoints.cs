using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.Messaging.Database;
using Dobropay.Receipts.Models;
using Dobropay.Receipts.Persistence;

namespace Dobropay.Receipts.Api;

public static class ReceiptsEndpoints
{
    public const string ReceiptNotFoundErrorCode = "receipt_not_found";
    public const string QueryRequiredErrorCode = "search_parameter_required";

    public static void MapReceiptsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(HealthRouteNames.Live, () => Results.Ok(new { status = HealthRouteNames.StatusLive }));

        app.MapGet(HealthRouteNames.Ready, async (IDatabaseHealthChecker healthChecker, CancellationToken ct) =>
            await healthChecker.CanConnectAsync(ct)
                ? Results.Ok(new { status = HealthRouteNames.StatusReady })
                : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

        app.MapGet(PublicRouteNames.ReceiptsLookup, HandleLookupAsync);
        app.MapGet(PublicRouteNames.ReceiptById, HandleGetByIdAsync);
        app.MapGet(InternalRouteNames.ShopReceipts, HandleShopReceiptsAsync);
    }

    public static async Task<IResult> HandleLookupAsync(
        HttpContext http,
        IReceiptsRepository repository,
        CancellationToken ct)
    {
        var query = http.Request.Query;
        var queryString = query["query"].ToString();
        var idString = query["id"].ToString();
        var paymentIdString = query["payment_id"].ToString();
        var fiscalDocumentNumberString = query["fiscal_document_number"].ToString();

        Guid? id = Guid.TryParse(idString, out var parsedId) ? parsedId : null;
        Guid? paymentId = Guid.TryParse(paymentIdString, out var parsedPaymentId) ? parsedPaymentId : null;
        long? fiscalDocNumber = long.TryParse(fiscalDocumentNumberString, out var parsedDocNum) ? parsedDocNum : null;

        if (!string.IsNullOrWhiteSpace(queryString))
        {
            var trimmed = queryString.Trim();
            if (Guid.TryParse(trimmed, out var queryGuid))
            {
                var byId = await repository.GetFiscalReceiptByIdAsync(queryGuid, ct);
                if (byId is not null)
                {
                    return Results.Ok(FiscalReceiptMapper.ToDto(byId));
                }

                var byPayment = await repository.LookupFiscalReceiptAsync(null, queryGuid, null, ct);
                if (byPayment is not null)
                {
                    return Results.Ok(FiscalReceiptMapper.ToDto(byPayment));
                }

                return Results.NotFound(new ReceiptErrorResponse(ReceiptNotFoundErrorCode));
            }

            if (long.TryParse(trimmed, out var queryDocNum))
            {
                var byDocNum = await repository.LookupFiscalReceiptAsync(null, null, queryDocNum, ct);
                if (byDocNum is not null)
                {
                    return Results.Ok(FiscalReceiptMapper.ToDto(byDocNum));
                }

                return Results.NotFound(new ReceiptErrorResponse(ReceiptNotFoundErrorCode));
            }

            return Results.NotFound(new ReceiptErrorResponse(ReceiptNotFoundErrorCode));
        }

        if (!id.HasValue && !paymentId.HasValue && !fiscalDocNumber.HasValue)
        {
            return Results.BadRequest(new ReceiptErrorResponse(QueryRequiredErrorCode));
        }

        var receipt = await repository.LookupFiscalReceiptAsync(id, paymentId, fiscalDocNumber, ct);
        return receipt is not null
            ? Results.Ok(FiscalReceiptMapper.ToDto(receipt))
            : Results.NotFound(new ReceiptErrorResponse(ReceiptNotFoundErrorCode));
    }

    public static async Task<IResult> HandleGetByIdAsync(
        Guid id,
        IReceiptsRepository repository,
        CancellationToken ct)
    {
        var receipt = await repository.GetFiscalReceiptByIdAsync(id, ct);
        return receipt is not null
            ? Results.Ok(FiscalReceiptMapper.ToDto(receipt))
            : Results.NotFound(new ReceiptErrorResponse(ReceiptNotFoundErrorCode));
    }

    public static async Task<IResult> HandleShopReceiptsAsync(
        HttpContext http,
        Guid shopId,
        IInternalKeyValidator keyValidator,
        IReceiptsRepository repository,
        CancellationToken ct)
    {
        if (!keyValidator.IsValid(http.Request.Headers[HttpHeaderNames.InternalKey]))
        {
            return Results.Unauthorized();
        }

        var receipts = await repository.ListFiscalReceiptsByShopAsync(shopId, ct);
        return Results.Ok(receipts.Select(FiscalReceiptMapper.ToDto).ToList());
    }
}
