using Dobropay.Contracts.Constants;
using Dobropay.Email.Api;
using Dobropay.Email.Models;
using Dobropay.Email.Services;
using Dobropay.Messaging.Security;

var builder = WebApplication.CreateBuilder(args);

string RequireConfig(string key) =>
    builder.Configuration[key] ?? throw new InvalidOperationException($"Missing configuration: {key}");

var smtpOptions = new SmtpOptions
{
    Host = RequireConfig(ConfigurationKeyNames.SmtpHost),
    Port = builder.Configuration.GetValue<int>(ConfigurationKeyNames.SmtpPort),
    From = RequireConfig(ConfigurationKeyNames.SmtpFrom)
};

builder.Services.AddSingleton(smtpOptions);
builder.Services.AddInternalSecurity(builder.Configuration);
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

var app = builder.Build();
app.MapEmailEndpoints();
app.Run();
