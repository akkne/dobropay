using System.Text.Json;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Events;
using Dobropay.Contracts.Versioning;
using Dobropay.Messaging.Kafka;
using Dobropay.Messaging.Metrics;
using Dobropay.Messaging.Redis;
using Dobropay.Notifications.Persistence;
using Dobropay.Notifications.Webhooks;
using StackExchange.Redis;

namespace Dobropay.Notifications.Workers;

public sealed class NotificationWorker(
    IServiceScopeFactory scopeFactory,
    IKafkaClientFactory kafkaClientFactory,
    IRateLimiter rateLimiter,
    IWebhookSignatureService signatureService,
    IHttpClientFactory httpClientFactory,
    ILogger<NotificationWorker> logger,
    IMetricsRecorder? metricsRecorder = null) : BackgroundService
{
    private const int ReceiveTimeoutMilliseconds = 100;
    private const int CycleDelayMilliseconds = 500;
    private const int RateLimitWindowSeconds = 1;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var subscriber = kafkaClientFactory.CreateSubscriber(
            KafkaContracts.NotificationsPaymentGroup,
            KafkaContracts.PaymentEvents);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = subscriber.Receive(TimeSpan.FromMilliseconds(ReceiveTimeoutMilliseconds));
                if (message is not null)
                {
                    await SaveEventAsync(message.RawPayload, stoppingToken);
                    subscriber.Commit(message);
                }

                await DeliverNextAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Notification cycle failed");
            }

            await Task.Delay(CycleDelayMilliseconds, stoppingToken);
        }
    }

    private async Task SaveEventAsync(string json, CancellationToken cancellationToken)
    {
        var envelope = JsonSerializer.Deserialize<EventEnvelope>(json)
            ?? throw new JsonException("Empty event");

        if (envelope.SchemaVersion != ContractVersions.EventSchema ||
            envelope.EventType is not (
                KafkaContracts.PaymentWaitingForCaptureType or
                KafkaContracts.PaymentSucceededType or
                KafkaContracts.PaymentCanceledType or
                KafkaContracts.RefundSucceededType or
                KafkaContracts.RefundCanceledType or
                KafkaContracts.ReceiptSucceededType))
        {
            throw new JsonException("Unsupported payment event");
        }

        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationsRepository>();

        await repository.SaveIncomingPaymentEventAsync(
            envelope.EventId,
            envelope.ShopId,
            envelope.PaymentId,
            json,
            cancellationToken);

        logger.LogInformation(
            "Queued webhook event {EventId} shop {ShopId} payment {PaymentId} correlation {CorrelationId}",
            envelope.EventId,
            envelope.ShopId,
            envelope.PaymentId,
            envelope.CorrelationId);
    }

    private async Task DeliverNextAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationsRepository>();

        var lease = await repository.AcquireNextDeliveryLeaseAsync(cancellationToken);
        if (lease is null)
        {
            return;
        }

        var envelope = JsonSerializer.Deserialize<EventEnvelope>(lease.EnvelopeJson)!;
        var bodyBytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            event_id = envelope.EventId,
            event_type = envelope.EventType,
            occurred_at = envelope.OccurredAt,
            payment = envelope.Payload
        });

        int? httpStatus = null;
        string outcome;

        try
        {
            var rateLimitKey = DomainConstants.WebhookRateLimitPrefix + lease.ShopId;
            var allowed = await rateLimiter.TryAcquireAsync(rateLimitKey, TimeSpan.FromSeconds(RateLimitWindowSeconds));
            if (!allowed)
            {
                metricsRecorder?.RecordWebhookDeliveryFailure(NotificationsSchemaConstants.ResultRateLimited);
                await repository.DeferDeliveryAsync(lease.DeliveryId, NotificationsSchemaConstants.ResultRateLimited, cancellationToken);
                return;
            }

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var signature = signatureService.ComputeSignature(lease.SigningSecret, timestamp, bodyBytes);

            using var request = new HttpRequestMessage(HttpMethod.Post, lease.Url)
            {
                Content = new ByteArrayContent(bodyBytes)
            };
            request.Content.Headers.ContentType = new(DomainConstants.ApplicationJsonContentType);
            request.Headers.Add(HttpHeaderNames.DobropayTimestamp, timestamp);
            request.Headers.Add(HttpHeaderNames.DobropaySignature, signature);

            using var response = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
            httpStatus = (int)response.StatusCode;
            outcome = response.IsSuccessStatusCode
                ? NotificationsSchemaConstants.ResultDelivered
                : NotificationsSchemaConstants.ResultHttpError;
        }
        catch (RedisConnectionException)
        {
            metricsRecorder?.RecordWebhookDeliveryFailure(NotificationsSchemaConstants.ResultRedisUnavailable);
            await repository.DeferDeliveryAsync(lease.DeliveryId, NotificationsSchemaConstants.ResultRedisUnavailable, cancellationToken);
            return;
        }
        catch (Exception exception)
        {
            outcome = exception.GetType().Name;
        }

        if (outcome != NotificationsSchemaConstants.ResultDelivered)
        {
            metricsRecorder?.RecordWebhookDeliveryFailure(outcome);
        }

        await repository.CompleteDeliveryAttemptAsync(
            lease.DeliveryId,
            lease.ShopId,
            lease.Attempts,
            httpStatus,
            outcome,
            cancellationToken);

        logger.LogInformation(
            "Webhook delivery {DeliveryId} event {EventId} shop {ShopId} result {Result} status {HttpStatus}",
            lease.DeliveryId,
            envelope.EventId,
            lease.ShopId,
            outcome,
            httpStatus);
    }
}
