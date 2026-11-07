using System.Text.Json.Serialization;

namespace Dobropay.MerchantAdmin.Models;

public sealed record ResolvedSessionIdentity(
    [property: JsonPropertyName("account_id")] Guid AccountId);
