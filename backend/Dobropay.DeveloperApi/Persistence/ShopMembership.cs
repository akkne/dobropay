using Dobropay.Contracts.Constants;

namespace Dobropay.DeveloperApi.Persistence;

public sealed class ShopMembership
{
    public Guid ShopId { get; set; }
    public Guid AccountId { get; set; }
    public string Role { get; set; } = DomainConstants.OwnerRole;
}
