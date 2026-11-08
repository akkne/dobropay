using System.Text.Json.Serialization;

namespace Dobropay.DeveloperApi.Models;

public sealed record CreateShopRequest(
    [property: JsonPropertyName("name")] string Name);
