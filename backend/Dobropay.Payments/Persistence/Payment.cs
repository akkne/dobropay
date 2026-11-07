using Dobropay.Contracts.Constants;
using Dobropay.Payments.Domain;

namespace Dobropay.Payments.Persistence;

public sealed class Payment
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public long Amount { get; set; }
    public string Currency { get; set; } = DomainConstants.DefaultCurrency;
    public string? ExternalOrderId { get; set; }
    public string Status { get; set; } = PaymentRules.Pending;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
