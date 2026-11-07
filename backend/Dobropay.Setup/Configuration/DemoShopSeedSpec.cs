namespace Dobropay.Setup.Configuration;

public sealed record DemoShopSeedSpec(
    Guid ShopId,
    string ShopName,
    Guid CredentialId,
    string RawApiKey,
    Guid SubscriptionId);
