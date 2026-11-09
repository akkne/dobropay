namespace Dobropay.Setup.Configuration;

public sealed class SetupOptions
{
    public required string AuthDbConnectionString { get; init; }
    public required string MerchantDevDbConnectionString { get; init; }
    public required string PaymentsDbConnectionString { get; init; }
    public required string NotificationsDbConnectionString { get; init; }
    public required string ReceiptsDbConnectionString { get; init; }

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
            ReceiptsDbConnectionString = Require(SetupEnvironmentVariableNames.ReceiptsDbConnectionString)
        };
    }
}
