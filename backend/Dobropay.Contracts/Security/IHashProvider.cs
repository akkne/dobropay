namespace Dobropay.Contracts.Security;

public interface IHashProvider
{
    string ComputeSha256Hex(string value);
    string ComputeHmacSha256Hex(string secret, ReadOnlySpan<byte> payload);
    bool FixedTimeEqualsAscii(string left, string right);
    bool FixedTimeEqualsSha256(string left, string right);
}
