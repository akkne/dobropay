using Dobropay.Contracts.Constants;
using Dobropay.Payments.Domain;

namespace Dobropay.Payments.Provider;

public sealed class SbpQrGenerator : ISbpQrGenerator
{
    private const int QrcIdHashLength = 16;

    public SbpQrPayload GenerateQr(Guid attemptId, long amount, string currency, DateTimeOffset expiresAt)
    {
        var qrcId = DomainConstants.SbpQrPrefix + attemptId.ToString("N")[..QrcIdHashLength];
        var payloadUrl = $"{DomainConstants.SbpQrBaseUrl}{qrcId}?amount={amount}&cur={currency}";
        return new SbpQrPayload(qrcId, payloadUrl, expiresAt);
    }
}
