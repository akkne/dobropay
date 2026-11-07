using Dobropay.Contracts.Constants;
using Dobropay.Messaging.Database;
using Dobropay.Messaging.Kafka;
using Dobropay.Messaging.Security;
using Dobropay.Receipts.Api;
using Dobropay.Receipts.Models;
using Dobropay.Receipts.Persistence;
using Dobropay.Receipts.Workers;

var builder = WebApplication.CreateBuilder(args);

string RequireConfig(string key) =>
    builder.Configuration[key] ?? throw new InvalidOperationException($"Missing configuration: {key}");

var options = new ReceiptsOptions
{
    EmailServiceBaseUrl = RequireConfig(ConfigurationKeyNames.ServiceEmailUrl),
    InternalServiceKey = RequireConfig(ConfigurationKeyNames.InternalServiceKey),
    DemoReceiptEmail = RequireConfig(ConfigurationKeyNames.DemoReceiptEmail)
};

builder.Services.AddSingleton(options);
builder.Services.AddPostgresDbContext<ReceiptsDbContext>(builder.Configuration);
builder.Services.AddKafkaMessaging(builder.Configuration);
builder.Services.AddInternalSecurity(builder.Configuration);
builder.Services.AddScoped<IReceiptsRepository, ReceiptsRepository>();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<ReceiptWorker>();

var app = builder.Build();
app.MapReceiptsEndpoints();
app.Run();
