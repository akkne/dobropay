using System.Text.Json.Serialization;

namespace Dobropay.DeveloperApi.Models;

public sealed record UpdateOnboardingRequest(
    [property: JsonPropertyName("inn")] string? Inn,
    [property: JsonPropertyName("ogrn")] string? Ogrn,
    [property: JsonPropertyName("legal_name")] string? LegalName,
    [property: JsonPropertyName("bank_bic")] string? BankBic,
    [property: JsonPropertyName("bank_account")] string? BankAccount,
    [property: JsonPropertyName("website_url")] string? WebsiteUrl);
