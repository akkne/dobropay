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

        app.MapGet(InternalRouteNames.ShopDeliveries, HandleListDeliveriesAsync);
        app.MapPost(InternalRouteNames.ShopDeliveryRetry, HandleRetryDeliveryAsync);
        app.MapGet(InternalRouteNames.ShopSubscriptions, HandleGetSubscriptionAsync);
        app.MapPost(InternalRouteNames.ShopSubscriptions, HandleSaveSubscriptionAsync);
        app.MapPut(InternalRouteNames.ShopSubscriptions, HandleSaveSubscriptionAsync);

        if (options.SandboxEnabled && !string.IsNullOrEmpty(options.WebhookSecret))
        {
            app.MapPost(PublicRouteNames.LocalWebhookReceiver, (
                HttpContext http,
                IWebhookSignatureService signatureService,
                INotificationsRepository repository,
                CancellationToken ct) => HandleLocalWebhookReceiverAsync(http, signatureService, repository, options.WebhookSecret, ct));

            app.MapGet(PublicRouteNames.LocalWebhooksReceived, HandleListLocalWebhooksAsync);
        }
    }

    public static async Task<IResult> HandleListDeliveriesAsync(
        HttpContext http,
        Guid shopId,
        IInternalKeyValidator keyValidator,
        INotificationsRepository repository,
        CancellationToken ct)
    {
        if (!keyValidator.IsValid(http.Request.Headers[HttpHeaderNames.InternalKey]))
        {
            return Results.Unauthorized();
        }

        var deliveries = await repository.ListDeliveriesByShopAsync(shopId, ct);
        return Results.Ok(deliveries);
    }

    public static async Task<IResult> HandleRetryDeliveryAsync(
        HttpContext http,
        Guid shopId,
        Guid deliveryId,
        IInternalKeyValidator keyValidator,
        INotificationsRepository repository,
        CancellationToken ct)
    {
        if (!keyValidator.IsValid(http.Request.Headers[HttpHeaderNames.InternalKey]))
        {
            return Results.Unauthorized();
        }

        var retried = await repository.RetryDeliveryAsync(shopId, deliveryId, ct);
        return retried
            ? Results.Ok(new { retried = true, delivery_id = deliveryId })
            : Results.NotFound();
    }

    public static async Task<IResult> HandleGetSubscriptionAsync(
        HttpContext http,
        Guid shopId,
        IInternalKeyValidator keyValidator,
        INotificationsRepository repository,
        CancellationToken ct)
    {
        if (!keyValidator.IsValid(http.Request.Headers[HttpHeaderNames.InternalKey]))
        {
            return Results.Unauthorized();
        }

        var subscription = await repository.GetSubscriptionByShopAsync(shopId, ct);
        return subscription is not null
            ? Results.Ok(new WebhookSubscriptionResponse(
                subscription.Id,
                subscription.ShopId,
                subscription.Url,
                subscription.SigningSecret,
                ParseEventTypes(subscription.EventTypes)))
            : Results.NotFound();
    }

    public static async Task<IResult> HandleSaveSubscriptionAsync(
        HttpContext http,
        Guid shopId,
        ConfigureWebhookSubscriptionRequest request,
        IInternalKeyValidator keyValidator,
        IWebhookUrlValidator urlValidator,
        INotificationsRepository repository,
        CancellationToken ct)
    {
        if (!keyValidator.IsValid(http.Request.Headers[HttpHeaderNames.InternalKey]))
        {
            return Results.Unauthorized();
        }

        var validation = await urlValidator.ValidateUrlAsync(request.Url, ct);
        if (!validation.IsValid)
        {
            return Results.BadRequest(new { error = validation.ErrorMessage });
        }

        var subscription = await repository.SaveSubscriptionAsync(
            shopId,
            request.Url,
            request.SigningSecret,
            request.EventTypes,
            ct);

        return Results.Ok(new WebhookSubscriptionResponse(
            subscription.Id,
            subscription.ShopId,
            subscription.Url,
            subscription.SigningSecret,
            ParseEventTypes(subscription.EventTypes)));
    }

    public static async Task<IResult> HandleLocalWebhookReceiverAsync(
        HttpContext http,
        IWebhookSignatureService signatureService,
        INotificationsRepository repository,
        string webhookSecret,
        CancellationToken ct)
    {
        using var stream = new MemoryStream();
        await http.Request.Body.CopyToAsync(stream, ct);
        var bodyBytes = stream.ToArray();

        var timestamp = http.Request.Headers[HttpHeaderNames.DobropayTimestamp].ToString();
        var signature = http.Request.Headers[HttpHeaderNames.DobropaySignature].ToString();

        if (!signatureService.VerifySignature(
            webhookSecret,
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
    }

    public static async Task<IResult> HandleListLocalWebhooksAsync(
        INotificationsRepository repository,
        CancellationToken ct)
    {
        var received = await repository.ListLocalReceivedWebhooksAsync(ct);
        return Results.Ok(received);
    }

    private static IReadOnlyList<string>? ParseEventTypes(string? eventTypes) =>
        string.IsNullOrWhiteSpace(eventTypes)
            ? null
            : eventTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
