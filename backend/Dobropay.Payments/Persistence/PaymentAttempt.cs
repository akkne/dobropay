using Dobropay.Payments.Domain;

namespace Dobropay.Payments.Persistence;

public sealed class PaymentAttempt
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid OperationKey { get; set; }
    public string? ExternalId { get; set; }
    public string State { get; set; } = PaymentRules.AttemptUnknown;
    public DateTimeOffset CreatedAt { get; set; }
}
