using Dobropay.Contracts.Constants;

namespace Dobropay.DeveloperApi.Persistence;

public sealed class ApiCredential
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string KeyHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Mode { get; set; } = DomainConstants.ApiKeyModeLive;
    public string KeyPrefix { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }
}
