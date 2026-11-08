using Dobropay.Contracts.Constants;
using Dobropay.Messaging.Database;
using Dobropay.Messaging.Kafka;
using Dobropay.Messaging.Security;
using Dobropay.Receipts.Api;
using Dobropay.Receipts.Models;
using Dobropay.Receipts.Persistence;
using Dobropay.Receipts.Services;
using Dobropay.Receipts.Workers;

var builder = WebApplication.CreateBuilder(args);

string RequireConfig(string key) =>
    builder.Configuration[key] ?? throw new InvalidOperationException($"Missing configuration: {key}");

var options = new ReceiptsOptions
{
    EmailServiceBaseUrl = RequireConfig(ConfigurationKeyNames.ServiceEmailUrl),
    InternalServiceKey = RequireConfig(ConfigurationKeyNames.InternalServiceKey),
    DemoReceiptEmail = RequireConfig(ConfigurationKeyNames.DemoReceiptEmail),
    PaymentsServiceBaseUrl = RequireConfig(ConfigurationKeyNames.ServicePaymentsUrl),
    FiscalProviderMode = builder.Configuration[ConfigurationKeyNames.FiscalProviderMode] ?? FiscalProviderModes.Sandbox,
    FiscalCloudKktBaseUrl = builder.Configuration[ConfigurationKeyNames.FiscalCloudKktBaseUrl],
    FiscalCloudKktApiKey = builder.Configuration[ConfigurationKeyNames.FiscalCloudKktApiKey],
    FiscalOfdBaseUrl = builder.Configuration[ConfigurationKeyNames.FiscalOfdBaseUrl]
};

builder.Services.AddSingleton(options);
builder.Services.AddPostgresDbContext<ReceiptsDbContext>(builder.Configuration);
builder.Services.AddKafkaMessaging(builder.Configuration);
builder.Services.AddInternalSecurity(builder.Configuration);
builder.Services.AddScoped<IReceiptsRepository, ReceiptsRepository>();
builder.Services.AddScoped<IPaymentReceiptContextClient, PaymentReceiptContextClient>();

if (string.Equals(options.FiscalProviderMode, FiscalProviderModes.CloudKkt, StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IFiscalProvider, CloudKktFiscalProvider>();
}
else
{
    builder.Services.AddScoped<IFiscalProvider, SandboxFiscalProvider>();
}

builder.Services.AddHttpClient();
builder.Services.AddHostedService<ReceiptWorker>();

var app = builder.Build();
app.MapReceiptsEndpoints();
app.Run();
