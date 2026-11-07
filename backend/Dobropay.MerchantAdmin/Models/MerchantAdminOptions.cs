namespace Dobropay.MerchantAdmin.Models;

public sealed class MerchantAdminOptions
{
    public required string AuthBaseUrl { get; init; }
    public required string DeveloperApiBaseUrl { get; init; }
    public required string PaymentsBaseUrl { get; init; }
    public required string NotificationsBaseUrl { get; init; }
    public required string ReceiptsBaseUrl { get; init; }
    public required string InternalServiceKey { get; init; }
}
