using System.Text.Json.Serialization;

namespace Dobropay.DeveloperApi.Models;

public sealed record SubmitOnboardingRequest(
    [property: JsonPropertyName("auto_approve")] bool? AutoApprove = null);
