namespace Dobropay.Payments.Domain;

public interface IPaymentProviderRouter
{
    IPaymentProvider Resolve(string? paymentMethodType);
    bool IsSupported(string? paymentMethodType);
}
