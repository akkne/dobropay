namespace Dobropay.Auth.Persistence;

public sealed class StaffSession
{
    public string TokenHash { get; set; } = string.Empty;
    public Guid AccountId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
