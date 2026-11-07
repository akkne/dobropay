using Dobropay.Contracts.Constants;
using Dobropay.Payments.Domain;

namespace Dobropay.Payments.Persistence;

public sealed class ProviderInbox
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid PaymentId { get; set; }
    public string ProviderEventId { get; set; } = string.Empty;
    public string Payload { get; set; } = DomainConstants.EmptyJsonObject;
    public string State { get; set; } = PaymentRules.InboxAccepted;
    public int Attempts { get; set; }
    public DateTimeOffset NextAt { get; set; }
    public string? Error { get; set; }
}
