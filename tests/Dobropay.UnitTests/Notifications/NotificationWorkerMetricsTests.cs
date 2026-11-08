using System.Net;
using System.Text.Json;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Events;
using Dobropay.Contracts.Security;
using Dobropay.Contracts.Versioning;
using Dobropay.Messaging.Metrics;
using Dobropay.Notifications.Persistence;
using Dobropay.Notifications.Webhooks;
using Dobropay.Notifications.Workers;
using Dobropay.UnitTests.MerchantAdmin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dobropay.UnitTests.Notifications;

public sealed class NotificationWorkerMetricsTests
{
    [Fact]
    public async Task NotificationWorker_WhenRateLimited_RecordsWebhookDeliveryFailureMetric()
    {
        var services = new ServiceCollection();
        var repository = new FakeNotificationsRepository();
        services.AddSingleton<INotificationsRepository>(repository);
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var shopId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var envelope = new EventEnvelope(
            eventId,
            KafkaContracts.PaymentSucceededType,
            ContractVersions.EventSchema,
            DateTimeOffset.UtcNow,
            shopId,
            paymentId,
            "corr-1",
            JsonSerializer.SerializeToElement(new { id = paymentId }));

        await repository.SaveSubscriptionAsync(shopId, "https://example.com/webhook", "test-secret", null, CancellationToken.None);
        await repository.SaveIncomingPaymentEventAsync(eventId, shopId, paymentId, JsonSerializer.Serialize(envelope), CancellationToken.None);

        var kafkaFactory = new FakeKafkaClientFactory();
        var rateLimiter = new FakeRateLimiter { Allowed = false };
        var signatureService = new WebhookSignatureService(new Sha256HashProvider());
        var httpClientFactory = new TestHttpClientFactory(new HttpClient(new TestHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)))));
        using var metricsRecorder = new OpenTelemetryMetricsRecorder();

        var worker = new NotificationWorker(
            scopeFactory,
            kafkaFactory,
            rateLimiter,
            signatureService,
            httpClientFactory,
            NullLogger<NotificationWorker>.Instance,
            metricsRecorder);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));
        await worker.StartAsync(cts.Token);
        await Task.Delay(250, CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(1, metricsRecorder.GetWebhookDeliveryFailureCount(NotificationsSchemaConstants.ResultRateLimited));
    }

    [Fact]
    public async Task NotificationWorker_WhenHttpError_RecordsWebhookDeliveryFailureMetric()
    {
        var services = new ServiceCollection();
        var repository = new FakeNotificationsRepository();
        services.AddSingleton<INotificationsRepository>(repository);
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var shopId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var envelope = new EventEnvelope(
            eventId,
            KafkaContracts.PaymentSucceededType,
            ContractVersions.EventSchema,
            DateTimeOffset.UtcNow,
            shopId,
            paymentId,
            "corr-2",
            JsonSerializer.SerializeToElement(new { id = paymentId }));

        await repository.SaveSubscriptionAsync(shopId, "https://example.com/webhook", "test-secret", null, CancellationToken.None);
        await repository.SaveIncomingPaymentEventAsync(eventId, shopId, paymentId, JsonSerializer.Serialize(envelope), CancellationToken.None);

        var kafkaFactory = new FakeKafkaClientFactory();
        var rateLimiter = new FakeRateLimiter { Allowed = true };
        var signatureService = new WebhookSignatureService(new Sha256HashProvider());
        var handler = new TestHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        var httpClientFactory = new TestHttpClientFactory(new HttpClient(handler));
        using var metricsRecorder = new OpenTelemetryMetricsRecorder();

        var worker = new NotificationWorker(
            scopeFactory,
            kafkaFactory,
            rateLimiter,
            signatureService,
            httpClientFactory,
            NullLogger<NotificationWorker>.Instance,
            metricsRecorder);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));
        await worker.StartAsync(cts.Token);
        await Task.Delay(250, CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(1, metricsRecorder.GetWebhookDeliveryFailureCount(NotificationsSchemaConstants.ResultHttpError));
    }
}
