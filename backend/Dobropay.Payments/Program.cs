using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.Messaging.Database;
using Dobropay.Messaging.Kafka;
using Dobropay.Messaging.Metrics;
using Dobropay.Messaging.Security;
using Dobropay.Payments.Api;
using Dobropay.Payments.Domain;
using Dobropay.Payments.Persistence;
using Dobropay.Payments.Provider;
using Dobropay.Payments.Services;
using Dobropay.Payments.Workers;

var builder = WebApplication.CreateBuilder(args);

string RequireConfig(string key) =>
    builder.Configuration[key] ?? throw new InvalidOperationException($"Missing configuration: {key}");

var options = new PaymentsOptions
{
    DeveloperApiBaseUrl = RequireConfig(ConfigurationKeyNames.ServiceDeveloperApiUrl),
    InternalServiceKey = RequireConfig(ConfigurationKeyNames.InternalServiceKey),
    CheckoutSecret = RequireConfig(ConfigurationKeyNames.CheckoutSecret),
    ProviderSecret = RequireConfig(ConfigurationKeyNames.ProviderSecret),
    SandboxEnabled = builder.Configuration.GetValue<bool>(ConfigurationKeyNames.SandboxEnabled),
    PublicBaseUrl = RequireConfig(ConfigurationKeyNames.SandboxPublicBaseUrl),
    InternalBaseUrl = RequireConfig(ConfigurationKeyNames.SandboxInternalBaseUrl)
};

var reconciliationOptions = new ReconciliationOptions
{
    IntervalSeconds = builder.Configuration.GetValue(ConfigurationKeyNames.ReconciliationIntervalSeconds, 10),
    AttemptStaleSeconds = builder.Configuration.GetValue(ConfigurationKeyNames.ReconciliationAttemptStaleSeconds, 15),
    PaymentExpirationMinutes = builder.Configuration.GetValue(ConfigurationKeyNames.PaymentExpirationMinutes, 30),
    CaptureExpirationMinutes = builder.Configuration.GetValue(ConfigurationKeyNames.PaymentCaptureExpirationMinutes, 30)
};

builder.Services.AddSingleton(options);
builder.Services.AddSingleton(reconciliationOptions);
builder.Services.AddDobropayMetrics();
builder.Services.AddPostgresDbContext<PaymentsDbContext>(builder.Configuration);
builder.Services.AddKafkaMessaging(builder.Configuration);
builder.Services.AddInternalSecurity(builder.Configuration);
builder.Services.AddSingleton<ICheckoutTokenGenerator>(provider =>
    new CheckoutTokenGenerator(options.CheckoutSecret, provider.GetRequiredService<IHashProvider>()));
builder.Services.AddSingleton<ISbpQrGenerator, SbpQrGenerator>();
builder.Services.AddSingleton<ICryptoExchangeRateProvider, CryptoExchangeRateProvider>();
builder.Services.AddSingleton(provider =>
    new SandboxProvider(options.ProviderSecret, provider.GetRequiredService<IHashProvider>()));
builder.Services.AddSingleton(provider =>
    new SbpPaymentProvider(
        options.ProviderSecret,
        provider.GetRequiredService<IHashProvider>(),
        provider.GetRequiredService<ISbpQrGenerator>()));
builder.Services.AddSingleton(provider =>
    new BankCardPaymentProvider(
        options.ProviderSecret,
        options.PublicBaseUrl,
        provider.GetRequiredService<IHashProvider>()));
builder.Services.AddSingleton(provider =>
    new CryptoPaymentProvider(
        options.ProviderSecret,
        provider.GetRequiredService<IHashProvider>(),
        provider.GetRequiredService<ICryptoExchangeRateProvider>()));
builder.Services.AddSingleton<IPaymentProvider>(provider => provider.GetRequiredService<SbpPaymentProvider>());
builder.Services.AddSingleton<IPaymentProvider>(provider => provider.GetRequiredService<BankCardPaymentProvider>());
builder.Services.AddSingleton<IPaymentProvider>(provider => provider.GetRequiredService<CryptoPaymentProvider>());
builder.Services.AddSingleton<IPaymentProvider>(provider => provider.GetRequiredService<SandboxProvider>());
builder.Services.AddSingleton<IPaymentProviderRouter, PaymentProviderRouter>();
builder.Services.AddScoped<IPaymentsRepository, PaymentsRepository>();
builder.Services.AddScoped<IRefundRepository, RefundRepository>();
builder.Services.AddScoped<IPaymentCaptureService, PaymentCaptureService>();
builder.Services.AddScoped<IRefundService, RefundService>();
builder.Services.AddScoped<IPaymentReconciliationService, PaymentReconciliationService>();
builder.Services.AddScoped<IPaymentReceiptContextService, PaymentReceiptContextService>();
builder.Services.AddHttpClient<IDeveloperApiClient, DeveloperApiClient>();
builder.Services.AddHostedService<PaymentEventWorker>();
builder.Services.AddHostedService<PaymentReconciliationWorker>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(swagger => swagger.OperationFilter<PaymentHeadersFilter>());

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.MapPaymentsEndpoints(options);
app.MapRefundsEndpoints();
app.MapProviderWebhookEndpoints();
app.Run();
