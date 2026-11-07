using Dobropay.Contracts.Constants;
using Dobropay.MerchantAdmin.Api;
using Dobropay.MerchantAdmin.Models;
using Dobropay.MerchantAdmin.Services;

var builder = WebApplication.CreateBuilder(args);

string RequireConfig(string key) =>
    builder.Configuration[key] ?? throw new InvalidOperationException($"Missing configuration: {key}");

var options = new MerchantAdminOptions
{
    AuthBaseUrl = RequireConfig(ConfigurationKeyNames.ServiceAuthUrl),
    DeveloperApiBaseUrl = RequireConfig(ConfigurationKeyNames.ServiceDeveloperApiUrl),
    PaymentsBaseUrl = RequireConfig(ConfigurationKeyNames.ServicePaymentsUrl),
    NotificationsBaseUrl = RequireConfig(ConfigurationKeyNames.ServiceNotificationsUrl),
    ReceiptsBaseUrl = RequireConfig(ConfigurationKeyNames.ServiceReceiptsUrl),
    InternalServiceKey = RequireConfig(ConfigurationKeyNames.InternalServiceKey)
};

builder.Services.AddSingleton(options);
builder.Services.AddHttpClient();
builder.Services.AddScoped<IMerchantAdminGatewayService, MerchantAdminGatewayService>();

var app = builder.Build();
app.MapMerchantAdminEndpoints(options);
app.Run();
