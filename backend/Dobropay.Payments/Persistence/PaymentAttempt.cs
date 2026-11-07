using Dobropay.Contracts.Constants;
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
    public string PaymentMethodType { get; set; } = PaymentMethodTypes.Sandbox;
    public string? ConfirmationPayload { get; set; } = null;
    public string? SbpQrcId { get; set; } = null;
    public string? SbpOperationId { get; set; } = null;
    public string? PayerBankId { get; set; } = null;
    public string? CryptoCurrency { get; set; } = null;
    public string? CryptoNetwork { get; set; } = null;
    public decimal? ExchangeRate { get; set; } = null;
    public string? CryptoAmount { get; set; } = null;
    public string? DepositAddress { get; set; } = null;
    public DateTimeOffset? QuoteExpiresAt { get; set; } = null;
    public string? TxHash { get; set; } = null;
    public int? Confirmations { get; set; } = null;
    public int? RequiredConfirmations { get; set; } = null;
    public DateTimeOffset CreatedAt { get; set; }
}
