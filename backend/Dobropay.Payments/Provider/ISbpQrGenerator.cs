using Dobropay.Payments.Domain;

namespace Dobropay.Payments.Provider;

public interface ISbpQrGenerator
{
    SbpQrPayload GenerateQr(Guid attemptId, long amount, string currency, DateTimeOffset expiresAt);
}
