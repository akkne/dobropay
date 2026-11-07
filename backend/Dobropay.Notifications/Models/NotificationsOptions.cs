namespace Dobropay.Notifications.Models;

public sealed class NotificationsOptions
{
    public required bool SandboxEnabled { get; init; }
    public string? WebhookSecret { get; init; }
}
