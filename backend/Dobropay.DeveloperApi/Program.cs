using Dobropay.Contracts.Constants;
using Dobropay.DeveloperApi.Api;
using Dobropay.DeveloperApi.Models;
using Dobropay.DeveloperApi.Persistence;
using Dobropay.DeveloperApi.Services;
using Dobropay.Messaging.Database;
using Dobropay.Messaging.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddPostgresDbContext<MerchantDbContext>(builder.Configuration);
builder.Services.AddInternalSecurity(builder.Configuration);

var developerApiOptions = new DeveloperApiOptions
{
    AuthBaseUrl = builder.Configuration[ConfigurationKeyNames.ServiceAuthUrl] ?? string.Empty,
    InternalServiceKey = builder.Configuration[ConfigurationKeyNames.InternalServiceKey] ?? string.Empty,
    SandboxEnabled = string.Equals(builder.Configuration[ConfigurationKeyNames.SandboxEnabled], "true", StringComparison.OrdinalIgnoreCase)
};
builder.Services.AddSingleton(developerApiOptions);

builder.Services.AddScoped<IMerchantRepository, MerchantRepository>();
builder.Services.AddScoped<IAuthSessionResolver, AuthSessionResolver>();
builder.Services.AddScoped<IDeveloperService, DeveloperService>();

var app = builder.Build();
app.MapDeveloperApiEndpoints();
app.Run();
