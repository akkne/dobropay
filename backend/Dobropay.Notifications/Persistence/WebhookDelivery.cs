namespace Dobropay.Notifications.Persistence;

public sealed class WebhookDelivery
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid EventId { get; set; }
    public Guid SubscriptionId { get; set; }
    public string State { get; set; } = NotificationsSchemaConstants.StatePending;
    public int Attempts { get; set; }
    public DateTimeOffset NextAt { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public string? LastResult { get; set; }
}
