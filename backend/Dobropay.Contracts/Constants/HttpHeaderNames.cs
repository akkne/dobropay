namespace Dobropay.Contracts.Constants;

public static class HttpHeaderNames
{
    public const string ApiKey = "X-Api-Key";
    public const string IdempotencyKey = "Idempotency-Key";
    public const string InternalKey = "X-Internal-Key";
    public const string ProviderSignature = "X-Provider-Signature";
    public const string DobropayTimestamp = "X-Dobropay-Timestamp";
    public const string DobropaySignature = "X-Dobropay-Signature";
    public const string Authorization = "Authorization";
}
