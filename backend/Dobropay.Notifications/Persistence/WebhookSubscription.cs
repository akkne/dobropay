namespace Dobropay.Notifications.Persistence;

public sealed class WebhookSubscription
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string SigningSecret { get; set; } = string.Empty;
}
