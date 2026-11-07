using Dobropay.Contracts.Constants;

namespace Dobropay.Notifications.Persistence;

public sealed class NotificationInbox
{
    public Guid EventId { get; set; }
    public Guid ShopId { get; set; }
    public Guid PaymentId { get; set; }
    public string Envelope { get; set; } = DomainConstants.EmptyJsonObject;
    public DateTimeOffset ReceivedAt { get; set; }
}
