using System.Text;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.Payments.Domain;
using Dobropay.Payments.Persistence;
using Dobropay.Payments.Provider;

namespace Dobropay.UnitTests;

public sealed class PaymentProvidersTests
{
    private const string TestSecret = "test-provider-secret";
    private const string TestPublicBaseUrl = "http://localhost:8080";

    [Fact]
    public void Router_ResolvesAllRegisteredProviders()
    {
        var hashProvider = new Sha256HashProvider();
        var sandbox = new SandboxProvider(TestSecret, hashProvider);
        var sbp = new SbpPaymentProvider(TestSecret, hashProvider, new SbpQrGenerator());
        var card = new BankCardPaymentProvider(TestSecret, TestPublicBaseUrl, hashProvider);
        var crypto = new CryptoPaymentProvider(TestSecret, hashProvider, new CryptoExchangeRateProvider());
        var router = new PaymentProviderRouter([sandbox, sbp, card, crypto]);

        Assert.True(router.IsSupported(null));
        Assert.True(router.IsSupported(PaymentMethodTypes.Sandbox));
        Assert.True(router.IsSupported(PaymentMethodTypes.Sbp));
        Assert.True(router.IsSupported(PaymentMethodTypes.BankCard));
        Assert.True(router.IsSupported(PaymentMethodTypes.Crypto));
        Assert.False(router.IsSupported("unknown"));

        Assert.Same(sandbox, router.Resolve(null));
        Assert.Same(sbp, router.Resolve("SBP"));
        Assert.Same(card, router.Resolve(PaymentMethodTypes.BankCard));
        Assert.Same(crypto, router.Resolve(PaymentMethodTypes.Crypto));
    }

    [Fact]
    public void SbpQrGenerator_CreatesDeterministicQrPayload()
    {
        var generator = new SbpQrGenerator();
        var attemptId = Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);

        var qr = generator.GenerateQr(attemptId, 199900, DomainConstants.DefaultCurrency, expiresAt);

        Assert.StartsWith(DomainConstants.SbpQrPrefix, qr.QrcId);
        Assert.Equal($"{DomainConstants.SbpQrBaseUrl}{qr.QrcId}?amount=199900&cur=RUB", qr.PayloadUrl);
        Assert.Equal(expiresAt, qr.ExpiresAt);
    }

    [Fact]
    public void BankCardPaymentProvider_Creates3dsChallengeAndDeterministicToken()
    {
        var hashProvider = new Sha256HashProvider();
        var provider = new BankCardPaymentProvider(TestSecret, TestPublicBaseUrl, hashProvider);
        var attemptId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var challenge = provider.Create3dsChallenge(attemptId, "chk_token_123");
        var token1 = provider.IssueProviderToken(paymentId, "220070", "1234");
        var token2 = provider.IssueProviderToken(paymentId, "220070", "1234");

        Assert.StartsWith(DomainConstants.CardAttemptPrefix, challenge.ExternalAttemptId);
        Assert.Contains("/checkout/chk_token_123?3ds=challenge&attempt=", challenge.ChallengeUrl);
        Assert.StartsWith(DomainConstants.CardProviderTokenPrefix, challenge.ProviderToken);
        Assert.Equal(token1, token2);
    }

    [Fact]
    public void CryptoExchangeRateProvider_CalculatesQuotesForSupportedNetworks()
    {
        var rateProvider = new CryptoExchangeRateProvider();
        var attemptId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        Assert.True(rateProvider.IsSupportedNetwork(CryptoNetworks.UsdtTrc20));
        Assert.True(rateProvider.IsSupportedNetwork(CryptoNetworks.Ton));
        Assert.True(rateProvider.IsSupportedNetwork(CryptoNetworks.Btc));
        Assert.False(rateProvider.IsSupportedNetwork("SOL"));

        var usdtQuote = rateProvider.CalculateQuote(attemptId, 95000, CryptoNetworks.UsdtTrc20, now);
        Assert.Equal(CryptoCurrencies.Usdt, usdtQuote.CryptoCurrency);
        Assert.Equal(95.00m, usdtQuote.ExchangeRate);
        Assert.Equal("10.000000", usdtQuote.CryptoAmount);
        Assert.Equal(19, usdtQuote.RequiredConfirmations);
        Assert.StartsWith($"{DomainConstants.CryptoDepositAddressPrefix}usdt_trc20_", usdtQuote.DepositAddress);

        var tonQuote = rateProvider.CalculateQuote(attemptId, 50000, CryptoNetworks.Ton, now);
        Assert.Equal(CryptoCurrencies.Ton, tonQuote.CryptoCurrency);
        Assert.Equal(1, tonQuote.RequiredConfirmations);

        var btcQuote = rateProvider.CalculateQuote(attemptId, 850000000, CryptoNetworks.Btc, now);
        Assert.Equal(CryptoCurrencies.Btc, btcQuote.CryptoCurrency);
        Assert.Equal("1.00000000", btcQuote.CryptoAmount);
        Assert.Equal(3, btcQuote.RequiredConfirmations);

        var body = Encoding.UTF8.GetBytes("{\"id\":\"evt-1\"}");
        var hashProvider = new Sha256HashProvider();
        var cryptoProvider = new CryptoPaymentProvider(TestSecret, hashProvider, rateProvider);
        var sig = hashProvider.ComputeHmacSha256Hex(TestSecret, body);
        Assert.True(cryptoProvider.VerifyEvent(body, sig));
    }

    [Fact]
    public async Task FakePaymentsRepository_StoresAndReturnsSavedPaymentMethodViews()
    {
        var repository = new FakePaymentsRepository();
        var shopId = Guid.NewGuid();
        var methodId = Guid.NewGuid();
        var savedAt = DateTimeOffset.UtcNow;

        await repository.AddSavedPaymentMethodAsync(
            new SavedPaymentMethod
            {
                Id = methodId,
                ShopId = shopId,
                Type = PaymentMethodTypes.BankCard,
                CardFirst6 = "220070",
                CardLast4 = "9876",
                ExpiryMonth = 8,
                ExpiryYear = 2029,
                CardBrand = DomainConstants.DefaultCardBrand,
                ProviderToken = "pm_tok_test123",
                SavedAt = savedAt
            },
            CancellationToken.None);

        var single = await repository.FindSavedPaymentMethodViewAsync(shopId, methodId, CancellationToken.None);
        var list = await repository.ListSavedPaymentMethodsAsync(shopId, 10, CancellationToken.None);

        Assert.NotNull(single);
        Assert.Equal("220070", single!.CardFirst6);
        Assert.Equal("9876", single.CardLast4);
        Assert.Single(list);
        Assert.Equal(methodId, list[0].Id);
    }
}
