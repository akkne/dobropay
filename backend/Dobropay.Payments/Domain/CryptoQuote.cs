namespace Dobropay.Payments.Domain;

public sealed record CryptoQuote(
    string Network,
    string CryptoCurrency,
    decimal ExchangeRate,
    string CryptoAmount,
    string DepositAddress,
    int RequiredConfirmations,
    DateTimeOffset QuoteExpiresAt);
