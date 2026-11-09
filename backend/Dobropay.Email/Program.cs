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
    From = RequireConfig(ConfigurationKeyNames.SmtpFrom),
    Username = RequireConfig(ConfigurationKeyNames.SmtpUsername),
    Password = RequireConfig(ConfigurationKeyNames.SmtpPassword),
    EnableSsl = builder.Configuration.GetValue<bool>(ConfigurationKeyNames.SmtpEnableSsl)
};

if (smtpOptions.Port is < 1 or > 65535 || !smtpOptions.EnableSsl ||
    string.IsNullOrWhiteSpace(smtpOptions.Host) || string.IsNullOrWhiteSpace(smtpOptions.From) ||
    string.IsNullOrWhiteSpace(smtpOptions.Username) || string.IsNullOrWhiteSpace(smtpOptions.Password))
{
    throw new InvalidOperationException("Valid authenticated TLS SMTP configuration is required");
}

builder.Services.AddSingleton(smtpOptions);
builder.Services.AddInternalSecurity(builder.Configuration);
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

var app = builder.Build();
app.MapEmailEndpoints();
app.Run();
