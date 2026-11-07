using System.Text.Json;
using Dobropay.Contracts.Constants;
using Dobropay.Notifications.Models;
using Microsoft.EntityFrameworkCore;

namespace Dobropay.Notifications.Persistence;

public sealed class NotificationsRepository(NotificationsDbContext dbContext) : INotificationsRepository
{
    private const int LeaseDurationSeconds = 30;
    private const int DeferDelaySeconds = 5;
    private const int MaxRetryDelaySeconds = 60;
    private const int MaxDeliveryAttempts = 5;

    public async Task<IReadOnlyList<WebhookDeliveryView>> ListDeliveriesByShopAsync(
        Guid shopId,
        CancellationToken cancellationToken) =>
        await dbContext.WebhookDeliveries
            .AsNoTracking()
            .Where(x => x.ShopId == shopId)
            .Select(x => new WebhookDeliveryView(x.EventId, x.State, x.Attempts, x.LastResult))
            .ToListAsync(cancellationToken);

    public async Task SaveIncomingPaymentEventAsync(
        Guid eventId,
        Guid shopId,
        Guid paymentId,
        string rawEnvelope,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.NotificationInbox
            .AsNoTracking()
            .AnyAsync(x => x.EventId == eventId, cancellationToken);
        if (exists)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        await dbContext.NotificationInbox.AddAsync(
            new NotificationInbox
            {
                EventId = eventId,
                ShopId = shopId,
                PaymentId = paymentId,
                Envelope = rawEnvelope,
                ReceivedAt = now
            },
            cancellationToken);

        var subscriptions = await dbContext.WebhookSubscriptions
            .AsNoTracking()
            .Where(x => x.ShopId == shopId)
            .ToListAsync(cancellationToken);

        foreach (var subscription in subscriptions)
        {
            await dbContext.WebhookDeliveries.AddAsync(
                new WebhookDelivery
                {
                    Id = Guid.NewGuid(),
                    ShopId = shopId,
                    EventId = eventId,
                    SubscriptionId = subscription.Id,
                    State = NotificationsSchemaConstants.StatePending,
                    Attempts = 0,
                    NextAt = now
                },
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<DeliveryLease?> AcquireNextDeliveryLeaseAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var candidate = await dbContext.WebhookDeliveries
            .Where(d =>
                (d.State == NotificationsSchemaConstants.StatePending && d.NextAt <= now) ||
                (d.State == NotificationsSchemaConstants.StateProcessing && d.LockedUntil < now))
            .OrderBy(d => d.NextAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (candidate is null)
        {
            return null;
        }

        var subscription = await dbContext.WebhookSubscriptions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                s => s.Id == candidate.SubscriptionId && s.ShopId == candidate.ShopId,
                cancellationToken);
        var inbox = await dbContext.NotificationInbox
            .AsNoTracking()
            .SingleOrDefaultAsync(
                i => i.EventId == candidate.EventId && i.ShopId == candidate.ShopId,
                cancellationToken);

        if (subscription is null || inbox is null)
        {
            return null;
        }

        candidate.State = NotificationsSchemaConstants.StateProcessing;
        candidate.Attempts += 1;
        candidate.LockedUntil = now.AddSeconds(LeaseDurationSeconds);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DeliveryLease(
            candidate.Id,
            candidate.ShopId,
            subscription.Url,
            subscription.SigningSecret,
            inbox.Envelope,
            candidate.Attempts);
    }

    public async Task DeferDeliveryAsync(Guid deliveryId, string reason, CancellationToken cancellationToken)
    {
        var delivery = await dbContext.WebhookDeliveries
            .SingleOrDefaultAsync(x => x.Id == deliveryId, cancellationToken);
        if (delivery is null)
        {
            return;
        }

        delivery.State = NotificationsSchemaConstants.StatePending;
        delivery.Attempts -= 1;
        delivery.LockedUntil = null;
        delivery.NextAt = DateTimeOffset.UtcNow.AddSeconds(DeferDelaySeconds);
        delivery.LastResult = reason;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteDeliveryAttemptAsync(
        Guid deliveryId,
        Guid shopId,
        int attempts,
        int? httpStatus,
        string outcome,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        await dbContext.WebhookDeliveryAttempts.AddAsync(
            new WebhookDeliveryAttempt
            {
                Id = Guid.NewGuid(),
                ShopId = shopId,
                DeliveryId = deliveryId,
                AttemptedAt = now,
                HttpStatus = httpStatus,
                Result = outcome
            },
            cancellationToken);

        var delivery = await dbContext.WebhookDeliveries
            .SingleOrDefaultAsync(x => x.Id == deliveryId, cancellationToken);
        if (delivery is not null)
        {
            var backoffSeconds = Math.Min(MaxRetryDelaySeconds, (int)Math.Pow(2, attempts));
            delivery.State = outcome == NotificationsSchemaConstants.ResultDelivered
                ? NotificationsSchemaConstants.StateDelivered
                : attempts >= MaxDeliveryAttempts
                    ? NotificationsSchemaConstants.StateFailed
                    : NotificationsSchemaConstants.StatePending;
            delivery.LockedUntil = null;
            delivery.NextAt = now.AddSeconds(backoffSeconds);
            delivery.LastResult = outcome;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> RecordLocalReceivedWebhookAsync(
        Guid eventId,
        string rawBody,
        CancellationToken cancellationToken)
    {
        var record = await dbContext.LocalReceiverEvents
            .SingleOrDefaultAsync(x => x.EventId == eventId, cancellationToken);

        if (record is null)
        {
            record = new LocalReceiverEvent
            {
                EventId = eventId,
                Attempts = 1,
                Verified = true,
                Body = rawBody
            };
            await dbContext.LocalReceiverEvents.AddAsync(record, cancellationToken);
        }
        else
        {
            record.Attempts += 1;
            record.Verified = true;
            record.Body = rawBody;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return record.Attempts;
    }

    public async Task<IReadOnlyList<LocalReceivedWebhookView>> ListLocalReceivedWebhooksAsync(
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.LocalReceiverEvents
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return rows
            .Select(x =>
            {
                using var doc = JsonDocument.Parse(x.Body ?? DomainConstants.EmptyJsonObject);
                return new LocalReceivedWebhookView(x.EventId, x.Attempts, x.Verified, doc.RootElement.Clone());
            })
            .ToList();
    }
}
