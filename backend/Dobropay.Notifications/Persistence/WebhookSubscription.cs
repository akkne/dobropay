using System.Text.Json;

namespace Dobropay.Notifications.Persistence;

public sealed class WebhookSubscription
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string SigningSecret { get; set; } = string.Empty;
    public string? EventTypes { get; set; }

    public bool MatchesEventType(string? eventType)
    {
        if (string.IsNullOrWhiteSpace(EventTypes))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            return false;
        }

        if (EventTypes.StartsWith('['))
        {
            try
            {
                var list = JsonSerializer.Deserialize<string[]>(EventTypes);
                return list is null || list.Length == 0 || list.Contains(eventType, StringComparer.OrdinalIgnoreCase);
            }
            catch (JsonException)
            {
            }
        }

        var types = EventTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return types.Length == 0 || types.Contains(eventType, StringComparer.OrdinalIgnoreCase);
    }
}
