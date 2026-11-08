using System.Net;
using Dobropay.Notifications.Models;
using Dobropay.Notifications.Webhooks;

namespace Dobropay.UnitTests.Notifications;

public sealed class WebhookUrlValidatorTests
{
    private static WebhookUrlValidator CreateValidator(bool sandboxEnabled = false) =>
        new(new NotificationsOptions
        {
            SandboxEnabled = sandboxEnabled,
            WebhookSecret = "test-secret"
        });

    [Theory]
    [InlineData("https://127.0.0.1/webhook")]
    [InlineData("https://127.0.1.1/webhook")]
    [InlineData("https://[::1]/webhook")]
    public async Task ValidateUrlAsync_BlocksLoopbackAddresses(string url)
    {
        var validator = CreateValidator(sandboxEnabled: false);

        var result = await validator.ValidateUrlAsync(url, CancellationToken.None);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("https://10.0.0.1/webhook")]
    [InlineData("https://10.254.1.2/webhook")]
    [InlineData("https://172.16.0.1/webhook")]
    [InlineData("https://172.31.255.254/webhook")]
    [InlineData("https://192.168.0.1/webhook")]
    [InlineData("https://192.168.100.50/webhook")]
    [InlineData("https://169.254.169.254/webhook")]
    [InlineData("https://0.0.0.0/webhook")]
    [InlineData("https://255.255.255.255/webhook")]
    public async Task ValidateUrlAsync_BlocksPrivateAndReservedIPv4(string url)
    {
        var validator = CreateValidator(sandboxEnabled: false);

        var result = await validator.ValidateUrlAsync(url, CancellationToken.None);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("https://[fc00::1]/webhook")]
    [InlineData("https://[fd00:1234::1]/webhook")]
    [InlineData("https://[fe80::1]/webhook")]
    [InlineData("https://[::ffff:192.168.1.1]/webhook")]
    [InlineData("https://[::ffff:10.0.0.1]/webhook")]
    public async Task ValidateUrlAsync_BlocksDisallowedIPv6(string url)
    {
        var validator = CreateValidator(sandboxEnabled: false);

        var result = await validator.ValidateUrlAsync(url, CancellationToken.None);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-valid-url")]
    [InlineData("relative/path/webhook")]
    [InlineData("ftp://example.com/webhook")]
    public async Task ValidateUrlAsync_RejectsInvalidFormat(string url)
    {
        var validator = CreateValidator(sandboxEnabled: false);

        var result = await validator.ValidateUrlAsync(url, CancellationToken.None);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateUrlAsync_RejectsHttp_WhenSandboxDisabled()
    {
        var validator = CreateValidator(sandboxEnabled: false);

        var result = await validator.ValidateUrlAsync("http://example.com/webhook", CancellationToken.None);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateUrlAsync_AllowsPublicHttpsIp()
    {
        var validator = CreateValidator(sandboxEnabled: false);

        var result = await validator.ValidateUrlAsync("https://8.8.8.8/webhook", CancellationToken.None);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("http://localhost:8080/local/webhooks/receiver")]
    [InlineData("http://127.0.0.1:8080/local/webhooks/receiver")]
    [InlineData("http://notifications:8080/local/webhooks/receiver")]
    [InlineData("https://notifications/local/webhooks/receiver")]
    public async Task ValidateUrlAsync_AllowsSandboxReceivers_WhenSandboxEnabled(string url)
    {
        var validator = CreateValidator(sandboxEnabled: true);

        var result = await validator.ValidateUrlAsync(url, CancellationToken.None);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("http://10.0.0.1/webhook")]
    [InlineData("http://192.168.1.1/webhook")]
    public async Task ValidateUrlAsync_BlocksOtherPrivateIps_EvenWhenSandboxEnabled(string url)
    {
        var validator = CreateValidator(sandboxEnabled: true);

        var result = await validator.ValidateUrlAsync(url, CancellationToken.None);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void IsDisallowedIpAddress_ChecksVariousRangesCorrectly()
    {
        Assert.True(WebhookUrlValidator.IsDisallowedIpAddress(IPAddress.Parse("127.0.0.1")));
        Assert.True(WebhookUrlValidator.IsDisallowedIpAddress(IPAddress.Parse("10.1.2.3")));
        Assert.True(WebhookUrlValidator.IsDisallowedIpAddress(IPAddress.Parse("172.20.0.1")));
        Assert.True(WebhookUrlValidator.IsDisallowedIpAddress(IPAddress.Parse("192.168.1.1")));
        Assert.True(WebhookUrlValidator.IsDisallowedIpAddress(IPAddress.Parse("169.254.1.1")));
        Assert.True(WebhookUrlValidator.IsDisallowedIpAddress(IPAddress.Parse("0.0.0.0")));
        Assert.True(WebhookUrlValidator.IsDisallowedIpAddress(IPAddress.Parse("255.255.255.255")));
        Assert.True(WebhookUrlValidator.IsDisallowedIpAddress(IPAddress.IPv6Loopback));
        Assert.True(WebhookUrlValidator.IsDisallowedIpAddress(IPAddress.Parse("fc00::1")));
        Assert.True(WebhookUrlValidator.IsDisallowedIpAddress(IPAddress.Parse("fe80::1")));
        Assert.False(WebhookUrlValidator.IsDisallowedIpAddress(IPAddress.Parse("8.8.8.8")));
        Assert.False(WebhookUrlValidator.IsDisallowedIpAddress(IPAddress.Parse("1.1.1.1")));
    }
}
