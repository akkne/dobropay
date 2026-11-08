using Dobropay.Contracts.Constants;
using Dobropay.Messaging.Database;
using Dobropay.Messaging.Kafka;
using Dobropay.Messaging.Metrics;
using Dobropay.Messaging.Redis;
using Dobropay.Messaging.Security;
using Dobropay.Notifications.Api;
using Dobropay.Notifications.Models;
using Dobropay.Notifications.Persistence;
using Dobropay.Notifications.Webhooks;
using Dobropay.Notifications.Workers;

var builder = WebApplication.CreateBuilder(args);

var sandboxEnabled = builder.Configuration.GetValue<bool>(ConfigurationKeyNames.SandboxEnabled);
var webhookSecret = sandboxEnabled
    ? builder.Configuration[ConfigurationKeyNames.WebhookSecret]
        ?? throw new InvalidOperationException($"Missing configuration: {ConfigurationKeyNames.WebhookSecret}")
    : null;

var options = new NotificationsOptions
{
    SandboxEnabled = sandboxEnabled,
    WebhookSecret = webhookSecret
};

builder.Services.AddSingleton(options);
builder.Services.AddDobropayMetrics();
builder.Services.AddPostgresDbContext<NotificationsDbContext>(builder.Configuration);
builder.Services.AddKafkaMessaging(builder.Configuration);
builder.Services.AddRedisRateLimiting(builder.Configuration);
builder.Services.AddInternalSecurity(builder.Configuration);
builder.Services.AddSingleton<IWebhookSignatureService, WebhookSignatureService>();
builder.Services.AddSingleton<IWebhookUrlValidator, WebhookUrlValidator>();
builder.Services.AddScoped<INotificationsRepository, NotificationsRepository>();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<NotificationWorker>();

var app = builder.Build();
app.MapNotificationsEndpoints(options);
app.Run();
