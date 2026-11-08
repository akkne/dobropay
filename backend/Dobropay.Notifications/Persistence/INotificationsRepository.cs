using Dobropay.Notifications.Models;

namespace Dobropay.Notifications.Persistence;

public interface INotificationsRepository
{
    Task<IReadOnlyList<WebhookDeliveryView>> ListDeliveriesByShopAsync(Guid shopId, CancellationToken cancellationToken);
    Task<bool> RetryDeliveryAsync(Guid shopId, Guid deliveryId, CancellationToken cancellationToken);
    Task<WebhookSubscription> SaveSubscriptionAsync(Guid shopId, string url, string? signingSecret, IReadOnlyList<string>? eventTypes, CancellationToken cancellationToken);
    Task<WebhookSubscription?> GetSubscriptionByShopAsync(Guid shopId, CancellationToken cancellationToken);
    Task SaveIncomingPaymentEventAsync(Guid eventId, Guid shopId, Guid paymentId, string rawEnvelope, CancellationToken cancellationToken);
    Task<DeliveryLease?> AcquireNextDeliveryLeaseAsync(CancellationToken cancellationToken);
    Task DeferDeliveryAsync(Guid deliveryId, string reason, CancellationToken cancellationToken);
    Task CompleteDeliveryAttemptAsync(Guid deliveryId, Guid shopId, int attempts, int? httpStatus, string outcome, CancellationToken cancellationToken);
    Task<int> RecordLocalReceivedWebhookAsync(Guid eventId, string rawBody, CancellationToken cancellationToken);
    Task<IReadOnlyList<LocalReceivedWebhookView>> ListLocalReceivedWebhooksAsync(CancellationToken cancellationToken);
}
