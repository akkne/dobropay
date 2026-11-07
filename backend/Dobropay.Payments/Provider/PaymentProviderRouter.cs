using Dobropay.Contracts.Constants;
using Dobropay.Payments.Domain;

namespace Dobropay.Payments.Provider;

public sealed class PaymentProviderRouter : IPaymentProviderRouter
{
    private readonly Dictionary<string, IPaymentProvider> _providers;

    public PaymentProviderRouter(IEnumerable<IPaymentProvider> providers)
    {
        _providers = new Dictionary<string, IPaymentProvider>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in providers)
        {
            _providers[provider.PaymentMethodType] = provider;
        }
    }

    public bool IsSupported(string? paymentMethodType)
    {
        var normalized = string.IsNullOrWhiteSpace(paymentMethodType)
            ? PaymentMethodTypes.Sandbox
            : paymentMethodType.Trim();
        return _providers.ContainsKey(normalized);
    }

    public IPaymentProvider Resolve(string? paymentMethodType)
    {
        var normalized = string.IsNullOrWhiteSpace(paymentMethodType)
            ? PaymentMethodTypes.Sandbox
            : paymentMethodType.Trim();
        return _providers.TryGetValue(normalized, out var provider)
            ? provider
            : throw new InvalidOperationException($"Unsupported payment method type: {normalized}");
    }
}
