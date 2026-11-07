using Dobropay.Contracts.Constants;

namespace Dobropay.Payments.Persistence;

public sealed class OutboxEvent
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid PaymentId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Envelope { get; set; } = DomainConstants.EmptyJsonObject;
    public DateTimeOffset OccurredAt { get; set; }
    public bool Published { get; set; }
}
