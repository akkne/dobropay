using Dobropay.Contracts.Constants;

namespace Dobropay.Payments.Persistence;

public sealed class SavedPaymentMethod
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string Type { get; set; } = PaymentMethodTypes.BankCard;
    public string CardFirst6 { get; set; } = string.Empty;
    public string CardLast4 { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string CardBrand { get; set; } = DomainConstants.DefaultCardBrand;
    public string ProviderToken { get; set; } = string.Empty;
    public DateTimeOffset SavedAt { get; set; }
}
