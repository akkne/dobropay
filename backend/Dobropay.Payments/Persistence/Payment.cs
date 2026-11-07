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
    public bool Capture { get; set; } = true;
    public long? CapturedAmount { get; set; } = null;
    public DateTimeOffset? CapturedAt { get; set; } = null;
    public long RefundedAmount { get; set; } = 0;
    public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddMinutes(30);
    public string? CancellationParty { get; set; } = null;
    public string? CancellationReason { get; set; } = null;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
