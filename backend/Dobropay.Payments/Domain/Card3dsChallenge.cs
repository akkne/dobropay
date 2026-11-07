namespace Dobropay.Payments.Domain;

public sealed record Card3dsChallenge(
    string ExternalAttemptId,
    string ChallengeUrl,
    string ProviderToken);
