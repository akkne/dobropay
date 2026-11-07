namespace Dobropay.Notifications.Persistence;

public sealed class WebhookDeliveryAttempt
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid DeliveryId { get; set; }
    public DateTimeOffset AttemptedAt { get; set; }
    public int? HttpStatus { get; set; }
    public string Result { get; set; } = string.Empty;
}
