using System.Text;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;

namespace Dobropay.Notifications.Webhooks;

public sealed class WebhookSignatureService(IHashProvider hashProvider) : IWebhookSignatureService
{
    private const string TimestampSeparator = ".";

    public string ComputeSignature(string secret, string timestamp, ReadOnlySpan<byte> body)
    {
        var prefix = Encoding.UTF8.GetBytes(timestamp + TimestampSeparator);
        var signedPayload = new byte[prefix.Length + body.Length];
        prefix.CopyTo(signedPayload, 0);
        body.CopyTo(signedPayload.AsSpan(prefix.Length));
        return AuthSchemeNames.Sha256Prefix + hashProvider.ComputeHmacSha256Hex(secret, signedPayload);
    }

    public bool VerifySignature(
        string secret,
        string timestamp,
        string signature,
        ReadOnlySpan<byte> body,
        int maxSkewSeconds)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (!long.TryParse(timestamp, out var seconds) || Math.Abs(now - seconds) > maxSkewSeconds)
        {
            return false;
        }

        var expected = ComputeSignature(secret, timestamp, body);
        return hashProvider.FixedTimeEqualsAscii(expected, signature);
    }
}
