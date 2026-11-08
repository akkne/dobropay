using Dobropay.Contracts.Constants;

namespace Dobropay.DeveloperApi.Persistence;

public sealed class ShopOnboardingProfile
{
    public Guid ShopId { get; set; }
    public string? Inn { get; set; }
    public string? Ogrn { get; set; }
    public string? LegalName { get; set; }
    public string? BankBic { get; set; }
    public string? BankAccount { get; set; }
    public string? WebsiteUrl { get; set; }
    public string VerificationStatus { get; set; } = VerificationStatuses.Draft;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
