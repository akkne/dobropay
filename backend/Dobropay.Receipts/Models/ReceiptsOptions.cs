using Dobropay.Contracts.Constants;

namespace Dobropay.Receipts.Models;

public sealed class ReceiptsOptions
{
    public required string EmailServiceBaseUrl { get; init; }
    public required string InternalServiceKey { get; init; }
    public required string PaymentsServiceBaseUrl { get; init; }
    public string FiscalProviderMode { get; init; } = FiscalProviderModes.Sandbox;
    public string? FiscalCloudKktBaseUrl { get; init; }
    public string? FiscalCloudKktApiKey { get; init; }
    public string? FiscalOfdBaseUrl { get; init; }
}
