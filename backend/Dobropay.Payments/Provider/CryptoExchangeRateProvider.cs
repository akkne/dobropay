using System.Globalization;
using Dobropay.Contracts.Constants;
using Dobropay.Payments.Domain;

namespace Dobropay.Payments.Provider;

public sealed class CryptoExchangeRateProvider : ICryptoExchangeRateProvider
{
    private const decimal KopecksPerRuble = 100m;
    private const decimal UsdtRubRate = 95.00m;
    private const decimal TonRubRate = 500.00m;
    private const decimal BtcRubRate = 8500000.00m;
    private const int UsdtRequiredConfirmations = 19;
    private const int TonRequiredConfirmations = 1;
    private const int BtcRequiredConfirmations = 3;
    private const int StandardCryptoDecimals = 6;
    private const int BtcCryptoDecimals = 8;
    private const int AddressHashLength = 16;
    private const int QuoteExpirationMinutes = 15;

    public bool IsSupportedNetwork(string? network) =>
        !string.IsNullOrWhiteSpace(network) &&
        (string.Equals(network.Trim(), CryptoNetworks.UsdtTrc20, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(network.Trim(), CryptoNetworks.Ton, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(network.Trim(), CryptoNetworks.Btc, StringComparison.OrdinalIgnoreCase));

    public CryptoQuote CalculateQuote(Guid attemptId, long amountKopecks, string network, DateTimeOffset now)
    {
        var trimmed = network.Trim();
        var (canonicalNetwork, currency, rate, requiredConfirmations, decimals) = ResolveNetworkSpec(trimmed);
        var rubAmount = amountKopecks / KopecksPerRuble;
        var cryptoDecimal = Math.Round(rubAmount / rate, decimals, MidpointRounding.AwayFromZero);
        var cryptoAmount = cryptoDecimal.ToString($"F{decimals}", CultureInfo.InvariantCulture);
        var networkSlug = canonicalNetwork.ToLowerInvariant().Replace('-', '_');
        var depositAddress = $"{DomainConstants.CryptoDepositAddressPrefix}{networkSlug}_{attemptId.ToString("N")[..AddressHashLength]}";
        return new CryptoQuote(
            canonicalNetwork,
            currency,
            rate,
            cryptoAmount,
            depositAddress,
            requiredConfirmations,
            now.AddMinutes(QuoteExpirationMinutes));
    }

    private static (string Network, string Currency, decimal Rate, int RequiredConfirmations, int Decimals) ResolveNetworkSpec(string network)
    {
        if (string.Equals(network, CryptoNetworks.UsdtTrc20, StringComparison.OrdinalIgnoreCase))
        {
            return (CryptoNetworks.UsdtTrc20, CryptoCurrencies.Usdt, UsdtRubRate, UsdtRequiredConfirmations, StandardCryptoDecimals);
        }

        if (string.Equals(network, CryptoNetworks.Ton, StringComparison.OrdinalIgnoreCase))
        {
            return (CryptoNetworks.Ton, CryptoCurrencies.Ton, TonRubRate, TonRequiredConfirmations, StandardCryptoDecimals);
        }

        if (string.Equals(network, CryptoNetworks.Btc, StringComparison.OrdinalIgnoreCase))
        {
            return (CryptoNetworks.Btc, CryptoCurrencies.Btc, BtcRubRate, BtcRequiredConfirmations, BtcCryptoDecimals);
        }

        throw new InvalidOperationException($"Unsupported crypto network: {network}");
    }
}
