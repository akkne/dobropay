using Dobropay.Payments.Domain;

namespace Dobropay.Payments.Provider;

public interface ICryptoExchangeRateProvider
{
    bool IsSupportedNetwork(string? network);
    CryptoQuote CalculateQuote(Guid attemptId, long amountKopecks, string network, DateTimeOffset now);
}
