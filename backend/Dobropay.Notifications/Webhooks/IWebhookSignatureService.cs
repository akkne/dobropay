namespace Dobropay.Notifications.Webhooks;

public interface IWebhookSignatureService
{
    string ComputeSignature(string secret, string timestamp, ReadOnlySpan<byte> body);
    bool VerifySignature(string secret, string timestamp, string signature, ReadOnlySpan<byte> body, int maxSkewSeconds);
}
