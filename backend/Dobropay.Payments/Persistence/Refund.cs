using Dobropay.Contracts.Constants;

namespace Dobropay.Payments.Persistence;

public sealed class Refund
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid PaymentId { get; set; }
    public long Amount { get; set; }
    public string Currency { get; set; } = DomainConstants.DefaultCurrency;
    public string Status { get; set; } = RefundStatuses.Pending;
    public string? Reason { get; set; }
    public string? ExternalRefundId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
