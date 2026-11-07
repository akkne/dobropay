namespace Dobropay.Payments.Services;

public sealed class PaymentsOptions
{
    public required string DeveloperApiBaseUrl { get; init; }
    public required string InternalServiceKey { get; init; }
    public required string CheckoutSecret { get; init; }
    public required string ProviderSecret { get; init; }
    public required bool SandboxEnabled { get; init; }
    public required string PublicBaseUrl { get; init; }
    public required string InternalBaseUrl { get; init; }
}
