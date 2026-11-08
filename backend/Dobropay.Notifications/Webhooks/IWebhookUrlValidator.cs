namespace Dobropay.Notifications.Webhooks;

public interface IWebhookUrlValidator
{
    Task<WebhookUrlValidationResult> ValidateUrlAsync(string url, CancellationToken cancellationToken = default);
}
