namespace Dobropay.DeveloperApi.Persistence;

public sealed class ApiCredential
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string KeyHash { get; set; } = string.Empty;
}
