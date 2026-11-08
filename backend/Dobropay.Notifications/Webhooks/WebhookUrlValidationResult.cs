namespace Dobropay.Notifications.Webhooks;

public sealed record WebhookUrlValidationResult(bool IsValid, string? ErrorMessage = null)
{
    public static WebhookUrlValidationResult Success() => new(true);

    public static WebhookUrlValidationResult Failure(string errorMessage) => new(false, errorMessage);
}
