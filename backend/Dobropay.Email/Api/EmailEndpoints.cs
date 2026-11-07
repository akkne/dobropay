using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.Email.Models;
using Dobropay.Email.Services;

namespace Dobropay.Email.Api;

public static class EmailEndpoints
{
    private const int MaxSubjectLength = 200;
    private const int MaxBodyLength = 10000;

    public static void MapEmailEndpoints(this WebApplication app)
    {
        app.MapGet(HealthRouteNames.Live, () => Results.Ok(new { status = HealthRouteNames.StatusLive }));
        app.MapGet(HealthRouteNames.Ready, () => Results.Ok(new { status = HealthRouteNames.StatusReady }));

        app.MapPost(InternalRouteNames.SendEmail, async (
            HttpContext http,
            EmailMessageRequest body,
            IInternalKeyValidator keyValidator,
            IEmailSender emailSender,
            CancellationToken ct) =>
        {
            if (!keyValidator.IsValid(http.Request.Headers[HttpHeaderNames.InternalKey]))
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(body.To) ||
                body.Subject.Length > MaxSubjectLength ||
                body.Text.Length > MaxBodyLength)
            {
                return Results.BadRequest();
            }

            await emailSender.SendAsync(body, ct);
            app.Logger.LogInformation(
                "Email accepted for receipt {ReceiptId} correlation {CorrelationId}",
                body.ReceiptId,
                http.TraceIdentifier);
            return Results.Accepted();
        });
    }
}
