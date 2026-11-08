namespace Dobropay.DeveloperApi.Models;

public sealed class DeveloperApiOptions
{
    public string AuthBaseUrl { get; set; } = string.Empty;
    public string InternalServiceKey { get; set; } = string.Empty;
    public bool SandboxEnabled { get; set; }
}
