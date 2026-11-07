namespace Dobropay.Setup.Configuration;

public sealed class SetupOptions
{
    public required string AuthDbConnectionString { get; init; }
    public required string MerchantDevDbConnectionString { get; init; }
    public required string PaymentsDbConnectionString { get; init; }
    public required string NotificationsDbConnectionString { get; init; }
    public required string ReceiptsDbConnectionString { get; init; }
    public required Guid DemoStaffId { get; init; }
    public required string DemoStaffEmail { get; init; }
    public required string DemoStaffPassword { get; init; }
    public required string DemoStaffSaltSeed { get; init; }
    public required string DemoWebhookUrl { get; init; }
    public required string WebhookSecret { get; init; }
    public required IReadOnlyList<DemoShopSeedSpec> DemoShops { get; init; }

    public static SetupOptions LoadFromEnvironment()
    {
        static string Require(string name) =>
            Environment.GetEnvironmentVariable(name)
            ?? throw new InvalidOperationException($"Missing environment variable: {name}");

        return new SetupOptions
        {
            AuthDbConnectionString = Require(SetupEnvironmentVariableNames.AuthDbConnectionString),
            MerchantDevDbConnectionString = Require(SetupEnvironmentVariableNames.MerchantDevDbConnectionString),
            PaymentsDbConnectionString = Require(SetupEnvironmentVariableNames.PaymentsDbConnectionString),
            NotificationsDbConnectionString = Require(SetupEnvironmentVariableNames.NotificationsDbConnectionString),
            ReceiptsDbConnectionString = Require(SetupEnvironmentVariableNames.ReceiptsDbConnectionString),
            DemoStaffId = Guid.Parse(Require(SetupEnvironmentVariableNames.DemoStaffId)),
            DemoStaffEmail = Require(SetupEnvironmentVariableNames.DemoStaffEmail),
            DemoStaffPassword = Require(SetupEnvironmentVariableNames.DemoStaffPassword),
            DemoStaffSaltSeed = Require(SetupEnvironmentVariableNames.DemoStaffSaltSeed),
            DemoWebhookUrl = Require(SetupEnvironmentVariableNames.DemoWebhookUrl),
            WebhookSecret = Require(SetupEnvironmentVariableNames.WebhookSecret),
            DemoShops =
            [
                new DemoShopSeedSpec(
                    Guid.Parse(Require(SetupEnvironmentVariableNames.DemoShopAId)),
                    Require(SetupEnvironmentVariableNames.DemoShopAName),
                    Guid.Parse(Require(SetupEnvironmentVariableNames.DemoCredentialAId)),
                    Require(SetupEnvironmentVariableNames.DemoKeyA),
                    Guid.Parse(Require(SetupEnvironmentVariableNames.DemoSubscriptionAId))),
                new DemoShopSeedSpec(
                    Guid.Parse(Require(SetupEnvironmentVariableNames.DemoShopBId)),
                    Require(SetupEnvironmentVariableNames.DemoShopBName),
                    Guid.Parse(Require(SetupEnvironmentVariableNames.DemoCredentialBId)),
                    Require(SetupEnvironmentVariableNames.DemoKeyB),
                    Guid.Parse(Require(SetupEnvironmentVariableNames.DemoSubscriptionBId)))
            ]
        };
    }
}
