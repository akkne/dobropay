using System.Text.Json;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Events;
using Dobropay.Notifications.Models;
using Dobropay.Notifications.Persistence;

namespace Dobropay.UnitTests.Notifications;

public sealed class FakeNotificationsRepository : INotificationsRepository
{
    public List<NotificationInbox> Inboxes { get; } = [];
    public List<WebhookSubscription> Subscriptions { get; } = [];
    public List<WebhookDelivery> Deliveries { get; } = [];
    public List<WebhookDeliveryAttempt> Attempts { get; } = [];
    public List<LocalReceiverEvent> LocalReceiverEvents { get; } = [];

    public Task<IReadOnlyList<WebhookDeliveryView>> ListDeliveriesByShopAsync(Guid shopId, CancellationToken cancellationToken)
    {
        var views = Deliveries
            .Where(x => x.ShopId == shopId)
            .Select(x => new WebhookDeliveryView(x.EventId, x.State, x.Attempts, x.LastResult))
            .ToList();
        return Task.FromResult<IReadOnlyList<WebhookDeliveryView>>(views);
    }

    public Task<bool> RetryDeliveryAsync(Guid shopId, Guid deliveryId, CancellationToken cancellationToken)
    {
        var delivery = Deliveries.SingleOrDefault(x => x.Id == deliveryId && x.ShopId == shopId);
        if (delivery is null)
        {
            return Task.FromResult(false);
        }

        delivery.State = NotificationsSchemaConstants.StatePending;
        delivery.NextAt = DateTimeOffset.UtcNow;
        delivery.LockedUntil = null;
        delivery.Attempts = 0;
        return Task.FromResult(true);
    }

    public Task<WebhookSubscription> SaveSubscriptionAsync(
        Guid shopId,
        string url,
        string? signingSecret,
        IReadOnlyList<string>? eventTypes,
        CancellationToken cancellationToken)
    {
        var subscription = Subscriptions.SingleOrDefault(x => x.ShopId == shopId);
        var eventTypesString = eventTypes is { Count: > 0 }
            ? string.Join(",", eventTypes.Select(e => e.Trim()).Where(e => !string.IsNullOrEmpty(e)))
            : null;

        if (subscription is null)
        {
            subscription = new WebhookSubscription
            {
                Id = Guid.NewGuid(),
                ShopId = shopId,
                Url = url,
                SigningSecret = signingSecret ?? "test-secret",
                EventTypes = eventTypesString
            };
            Subscriptions.Add(subscription);
        }
        else
        {
            subscription.Url = url;
            if (!string.IsNullOrWhiteSpace(signingSecret))
            {
                subscription.SigningSecret = signingSecret;
            }
            subscription.EventTypes = eventTypesString;
        }

        return Task.FromResult(subscription);
    }

    public Task<WebhookSubscription?> GetSubscriptionByShopAsync(Guid shopId, CancellationToken cancellationToken)
    {
        var subscription = Subscriptions.SingleOrDefault(x => x.ShopId == shopId);
        return Task.FromResult(subscription);
    }

    public Task SaveIncomingPaymentEventAsync(
        Guid eventId,
        Guid shopId,
        Guid paymentId,
        string rawEnvelope,
        CancellationToken cancellationToken)
    {
        if (Inboxes.Any(x => x.EventId == eventId))
        {
            return Task.CompletedTask;
        }

        var now = DateTimeOffset.UtcNow;
        Inboxes.Add(new NotificationInbox
        {
            EventId = eventId,
            ShopId = shopId,
            PaymentId = paymentId,
            Envelope = rawEnvelope,
            ReceivedAt = now
        });

        string? eventType = null;
        try
        {
            var envelope = JsonSerializer.Deserialize<EventEnvelope>(rawEnvelope);
            eventType = envelope?.EventType;
        }
        catch (JsonException)
        {
        }

        var subscriptions = Subscriptions.Where(x => x.ShopId == shopId).ToList();
        foreach (var subscription in subscriptions)
        {
            if (!subscription.MatchesEventType(eventType))
            {
                continue;
            }

            Deliveries.Add(new WebhookDelivery
            {
                Id = Guid.NewGuid(),
                ShopId = shopId,
                EventId = eventId,
                SubscriptionId = subscription.Id,
                State = NotificationsSchemaConstants.StatePending,
                Attempts = 0,
                NextAt = now
            });
        }

        return Task.CompletedTask;
    }

    public Task<DeliveryLease?> AcquireNextDeliveryLeaseAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var candidate = Deliveries
            .Where(d => (d.State == NotificationsSchemaConstants.StatePending && d.NextAt <= now) ||
                        (d.State == NotificationsSchemaConstants.StateProcessing && d.LockedUntil < now))
            .OrderBy(d => d.NextAt)
            .FirstOrDefault();

        if (candidate is null)
        {
            return Task.FromResult<DeliveryLease?>(null);
        }

        var subscription = Subscriptions.SingleOrDefault(s => s.Id == candidate.SubscriptionId && s.ShopId == candidate.ShopId);
        var inbox = Inboxes.SingleOrDefault(i => i.EventId == candidate.EventId && i.ShopId == candidate.ShopId);
        if (subscription is null || inbox is null)
        {
            return Task.FromResult<DeliveryLease?>(null);
        }

        candidate.State = NotificationsSchemaConstants.StateProcessing;
        candidate.Attempts += 1;
        candidate.LockedUntil = now.AddSeconds(30);

        return Task.FromResult<DeliveryLease?>(new DeliveryLease(
            candidate.Id,
            candidate.ShopId,
            subscription.Url,
            subscription.SigningSecret,
            inbox.Envelope,
            candidate.Attempts));
    }

    public Task DeferDeliveryAsync(Guid deliveryId, string reason, CancellationToken cancellationToken)
    {
        var delivery = Deliveries.SingleOrDefault(x => x.Id == deliveryId);
        if (delivery is not null)
        {
            delivery.State = NotificationsSchemaConstants.StatePending;
            delivery.Attempts -= 1;
            delivery.LockedUntil = null;
            delivery.NextAt = DateTimeOffset.UtcNow.AddSeconds(5);
            delivery.LastResult = reason;
        }
        return Task.CompletedTask;
    }

    public Task CompleteDeliveryAttemptAsync(
        Guid deliveryId,
        Guid shopId,
        int attempts,
        int? httpStatus,
        string outcome,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        Attempts.Add(new WebhookDeliveryAttempt
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            DeliveryId = deliveryId,
            AttemptedAt = now,
            HttpStatus = httpStatus,
            Result = outcome
        });

        var delivery = Deliveries.SingleOrDefault(x => x.Id == deliveryId);
        if (delivery is not null)
        {
            delivery.State = outcome == NotificationsSchemaConstants.ResultDelivered
                ? NotificationsSchemaConstants.StateDelivered
                : attempts >= 5
                    ? NotificationsSchemaConstants.StateFailed
                    : NotificationsSchemaConstants.StatePending;
            delivery.LockedUntil = null;
            delivery.NextAt = now.AddSeconds(Math.Min(60, (int)Math.Pow(2, attempts)));
            delivery.LastResult = outcome;
        }
        return Task.CompletedTask;
    }

    public Task<int> RecordLocalReceivedWebhookAsync(Guid eventId, string rawBody, CancellationToken cancellationToken)
    {
        var item = LocalReceiverEvents.SingleOrDefault(x => x.EventId == eventId);
        if (item is null)
        {
            item = new LocalReceiverEvent { EventId = eventId, Attempts = 1, Verified = true, Body = rawBody };
            LocalReceiverEvents.Add(item);
        }
        else
        {
            item.Attempts += 1;
            item.Verified = true;
            item.Body = rawBody;
        }
        return Task.FromResult(item.Attempts);
    }

    public Task<IReadOnlyList<LocalReceivedWebhookView>> ListLocalReceivedWebhooksAsync(CancellationToken cancellationToken)
    {
        var views = LocalReceiverEvents
            .Select(x =>
            {
                using var doc = JsonDocument.Parse(x.Body ?? DomainConstants.EmptyJsonObject);
                return new LocalReceivedWebhookView(x.EventId, x.Attempts, x.Verified, doc.RootElement.Clone());
            })
            .ToList();
        return Task.FromResult<IReadOnlyList<LocalReceivedWebhookView>>(views);
    }
}
