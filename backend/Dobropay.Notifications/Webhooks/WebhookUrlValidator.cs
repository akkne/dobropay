using System.Net;
using System.Net.Sockets;
using Dobropay.Notifications.Models;

namespace Dobropay.Notifications.Webhooks;

public sealed class WebhookUrlValidator(NotificationsOptions options) : IWebhookUrlValidator
{
    private static readonly HashSet<string> SandboxAllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "localhost",
        "127.0.0.1",
        "::1",
        "notifications"
    };

    public async Task<WebhookUrlValidationResult> ValidateUrlAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return WebhookUrlValidationResult.Failure("URL cannot be empty.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return WebhookUrlValidationResult.Failure("Invalid URL format.");
        }

        var isHttps = string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        var isHttp = string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);

        if (!isHttps && !(options.SandboxEnabled && isHttp))
        {
            return WebhookUrlValidationResult.Failure("URL scheme must be HTTPS.");
        }

        if (options.SandboxEnabled && SandboxAllowedHosts.Contains(uri.Host))
        {
            return WebhookUrlValidationResult.Success();
        }

        IPAddress[] addresses;
        if (IPAddress.TryParse(uri.Host, out var directIp))
        {
            addresses = [directIp];
        }
        else
        {
            try
            {
                addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken);
            }
            catch (Exception exception)
            {
                return WebhookUrlValidationResult.Failure($"DNS resolution failed: {exception.Message}");
            }
        }

        if (addresses.Length == 0)
        {
            return WebhookUrlValidationResult.Failure("Host could not be resolved.");
        }

        foreach (var address in addresses)
        {
            if (IsDisallowedIpAddress(address))
            {
                return WebhookUrlValidationResult.Failure("Disallowed or private IP address.");
            }
        }

        return WebhookUrlValidationResult.Success();
    }

    public static bool IsDisallowedIpAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            var b0 = bytes[0];
            var b1 = bytes[1];
            var b2 = bytes[2];
            var b3 = bytes[3];

            if (b0 == 0)
            {
                return true;
            }

            if (b0 == 10)
            {
                return true;
            }

            if (b0 == 127)
            {
                return true;
            }

            if (b0 == 169 && b1 == 254)
            {
                return true;
            }

            if (b0 == 172 && b1 >= 16 && b1 <= 31)
            {
                return true;
            }

            if (b0 == 192 && b1 == 168)
            {
                return true;
            }

            if (b0 == 255 && b1 == 255 && b2 == 255 && b3 == 255)
            {
                return true;
            }

            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (IPAddress.IPv6Loopback.Equals(address) || IPAddress.IPv6Any.Equals(address))
            {
                return true;
            }

            var bytes = address.GetAddressBytes();
            if ((bytes[0] & 0xfe) == 0xfc)
            {
                return true;
            }

            if (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80)
            {
                return true;
            }

            return false;
        }

        return true;
    }
}
