using System.Security.Cryptography;
using System.Text;

namespace Dobropay.Contracts.Security;

public sealed class Sha256HashProvider : IHashProvider
{
    public string ComputeSha256Hex(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    public string ComputeHmacSha256Hex(string secret, ReadOnlySpan<byte> payload) =>
        Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), payload)).ToLowerInvariant();

    public bool FixedTimeEqualsAscii(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(left), Encoding.ASCII.GetBytes(right));

    public bool FixedTimeEqualsSha256(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(left)),
            SHA256.HashData(Encoding.UTF8.GetBytes(right)));
}
