using System.Text.Json.Serialization;

namespace Dobropay.Notifications.Models;

public sealed class ConfigureWebhookSubscriptionRequest
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("signing_secret")]
    public string? SigningSecret { get; set; }

    [JsonPropertyName("signingSecret")]
    public string? SigningSecretAlt
    {
        set => SigningSecret ??= value;
    }

    [JsonPropertyName("event_types")]
    public IReadOnlyList<string>? EventTypes { get; set; }

    [JsonPropertyName("eventTypes")]
    public IReadOnlyList<string>? EventTypesAlt
    {
        set => EventTypes ??= value;
    }
}
