using System.Text;
using System.Text.Json;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.Messaging.Database;
using Dobropay.Notifications.Models;
using Dobropay.Notifications.Persistence;
using Dobropay.Notifications.Webhooks;

namespace Dobropay.Notifications.Api;

public static class NotificationsEndpoints
{
    private const int MaxTimestampSkewSeconds = 300;
    private const string EventIdPropertyName = "event_id";

    public static void MapNotificationsEndpoints(this IEndpointRouteBuilder app, NotificationsOptions options)
    {
        app.MapGet(HealthRouteNames.Live, () => Results.Ok(new { status = HealthRouteNames.StatusLive }));

        app.MapGet(HealthRouteNames.Ready, async (IDatabaseHealthChecker healthChecker, CancellationToken ct) =>
            await healthChecker.CanConnectAsync(ct)
                ? Results.Ok(new { status = HealthRouteNames.StatusReady })
                : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

        app.MapGet(InternalRouteNames.ShopDeliveries, async (
            HttpContext http,
            Guid shopId,
            IInternalKeyValidator keyValidator,
            INotificationsRepository repository,
            CancellationToken ct) =>
        {
            if (!keyValidator.IsValid(http.Request.Headers[HttpHeaderNames.InternalKey]))
            {
                return Results.Unauthorized();
            }

            var deliveries = await repository.ListDeliveriesByShopAsync(shopId, ct);
            return Results.Ok(deliveries);
        });

        if (options.SandboxEnabled && !string.IsNullOrEmpty(options.WebhookSecret))
        {
            app.MapPost(PublicRouteNames.LocalWebhookReceiver, async (
                HttpContext http,
                IWebhookSignatureService signatureService,
                INotificationsRepository repository,
                CancellationToken ct) =>
            {
                using var stream = new MemoryStream();
                await http.Request.Body.CopyToAsync(stream, ct);
                var bodyBytes = stream.ToArray();

                var timestamp = http.Request.Headers[HttpHeaderNames.DobropayTimestamp].ToString();
                var signature = http.Request.Headers[HttpHeaderNames.DobropaySignature].ToString();

                if (!signatureService.VerifySignature(
                    options.WebhookSecret,
                    timestamp,
                    signature,
                    bodyBytes,
                    MaxTimestampSkewSeconds))
                {
                    return Results.Unauthorized();
                }

                Guid eventId;
                try
                {
                    using var doc = JsonDocument.Parse(bodyBytes);
                    eventId = doc.RootElement.GetProperty(EventIdPropertyName).GetGuid();
                }
                catch (Exception exception) when (exception is JsonException or KeyNotFoundException or FormatException)
                {
                    return Results.BadRequest();
                }

                var attempts = await repository.RecordLocalReceivedWebhookAsync(
                    eventId,
                    Encoding.UTF8.GetString(bodyBytes),
                    ct);
                return attempts == 1
                    ? Results.StatusCode(StatusCodes.Status503ServiceUnavailable)
                    : Results.Ok(new { received = true, event_id = eventId });
            });

            app.MapGet(PublicRouteNames.LocalWebhooksReceived, async (
                INotificationsRepository repository,
                CancellationToken ct) =>
            {
                var received = await repository.ListLocalReceivedWebhooksAsync(ct);
                return Results.Ok(received);
            });
        }
    }
}
