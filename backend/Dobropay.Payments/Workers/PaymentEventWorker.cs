using System.Text.Json;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Events;
using Dobropay.Contracts.Versioning;
using Dobropay.Messaging.Database;
using Dobropay.Messaging.Kafka;
using Dobropay.Messaging.Metrics;
using Dobropay.Payments.Domain;
using Dobropay.Payments.Persistence;

namespace Dobropay.Payments.Workers;

public sealed class PaymentEventWorker(
    IServiceScopeFactory scopeFactory,
    IKafkaClientFactory kafkaClientFactory,
    IEventPublisher publisher,
    ILogger<PaymentEventWorker> logger,
    IMetricsRecorder? metricsRecorder = null) : BackgroundService
{
    private const int ReceiveTimeoutMilliseconds = 100;
    private const int CycleDelayMilliseconds = 500;
    private const int MaxWaitAttempts = 5;
    private const int HoldLifetimeMinutes = 30;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var subscriber = kafkaClientFactory.CreateSubscriber(
            KafkaContracts.PaymentsProviderGroup,
            KafkaContracts.ProviderEvents);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishNextOutboxAsync(stoppingToken);
                var message = subscriber.Receive(TimeSpan.FromMilliseconds(ReceiveTimeoutMilliseconds));
                if (message is not null && await HandleProviderEventAsync(message.RawPayload, stoppingToken))
                {
                    subscriber.Commit(message);
                }
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Payment event cycle failed");
            }

            await Task.Delay(CycleDelayMilliseconds, stoppingToken);
        }
    }

    private async Task PublishNextOutboxAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPaymentsRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var pendingCount = await repository.CountUnpublishedOutboxAsync(cancellationToken);
        metricsRecorder?.RecordOutboxPendingCount(pendingCount);

        var nextEvent = await repository.FindNextUnpublishedOutboxAsync(cancellationToken);
        if (nextEvent is null)
        {
            return;
        }

        var envelope = JsonSerializer.Deserialize<EventEnvelope>(nextEvent.Envelope);
        if (envelope is null)
        {
            return;
        }

        await publisher.PublishAsync(nextEvent.Topic, envelope, cancellationToken);
        nextEvent.Published = true;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var remainingCount = Math.Max(0, pendingCount - 1);
        metricsRecorder?.RecordOutboxPendingCount(remainingCount);

        logger.LogInformation(
            "Published event {EventId} payment {PaymentId} shop {ShopId} correlation {CorrelationId}",
            envelope.EventId,
            envelope.PaymentId,
            envelope.ShopId,
            envelope.CorrelationId);
    }

    private async Task<bool> HandleProviderEventAsync(string json, CancellationToken cancellationToken)
    {
        EventEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<EventEnvelope>(json);
        }
        catch (JsonException)
        {
            return false;
        }

        if (envelope is null)
        {
            return false;
        }

        if (envelope.SchemaVersion != ContractVersions.EventSchema ||
            envelope.EventType != KafkaContracts.ProviderEventAcceptedType)
        {
            await publisher.PublishAsync(
                KafkaContracts.DeadLetterQueue(KafkaContracts.PaymentsProviderConsumerName),
                envelope,
                cancellationToken);
            return true;
        }

        var incoming = envelope.Payload.Deserialize<ProviderEvent>(JsonSerializerOptions.Web);
        if (incoming is null)
        {
            return false;
        }

        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPaymentsRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var inbox = await repository.FindTrackedInboxAsync(envelope.EventId, envelope.ShopId, cancellationToken);
        if (inbox is null || inbox.State != PaymentRules.InboxAccepted)
        {
            return true;
        }

        var attempt = await repository.FindTrackedAttemptByIdAsync(envelope.ShopId, incoming.AttemptId, cancellationToken);
        var payment = await repository.FindTrackedPaymentByIdAsync(envelope.ShopId, envelope.PaymentId, cancellationToken);

        AttemptSnapshot? snapshot = null;
        if (attempt is not null && payment is not null && attempt.PaymentId == payment.Id)
        {
            snapshot = new AttemptSnapshot(
                envelope.ShopId,
                payment.Id,
                attempt.ExternalId,
                payment.Amount,
                payment.Currency,
                payment.Status);
        }

        var decision = PaymentConfirmation.Decide(incoming, snapshot);
        if (decision == ConfirmationDecision.WaitForAttempt && inbox.Attempts < MaxWaitAttempts)
        {
            inbox.Attempts += 1;
            inbox.Error = ErrorCodes.AttemptNotReady;
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return false;
        }

        if (decision == ConfirmationDecision.Apply && payment is not null && attempt is not null && snapshot is not null)
        {
            var now = DateTimeOffset.UtcNow;
            var targetStatus = PaymentRules.ResolveAttemptOutcome(incoming.Outcome, payment.Capture);
            payment.Status = targetStatus;
            payment.UpdatedAt = now;
            attempt.State = incoming.Outcome;
            metricsRecorder?.RecordPayment(targetStatus, snapshot.Currency);

            if (targetStatus == PaymentRules.Succeeded)
            {
                payment.CapturedAmount = payment.Amount;
                payment.CapturedAt = now;
            }
            else if (targetStatus == PaymentRules.WaitingForCapture)
            {
                payment.ExpiresAt = now.AddMinutes(HoldLifetimeMinutes);
            }
            else if (targetStatus == PaymentRules.Canceled)
            {
                payment.CancellationParty = PaymentCancellationParties.PaymentNetwork;
                payment.CancellationReason = PaymentCancellationReasons.ProviderDeclined;
            }

            var payload = JsonSerializer.SerializeToElement(new
            {
                id = envelope.PaymentId,
                status = targetStatus,
                amount = snapshot.Amount,
                currency = snapshot.Currency
            });

            var businessEnvelope = new EventEnvelope(
                Guid.NewGuid(),
                KafkaContracts.PaymentEventPrefix + targetStatus,
                ContractVersions.EventSchema,
                now,
                envelope.ShopId,
                envelope.PaymentId,
                envelope.CorrelationId,
                payload);

            await repository.AddOutboxEventAsync(
                new OutboxEvent
                {
                    Id = businessEnvelope.EventId,
                    ShopId = businessEnvelope.ShopId,
                    PaymentId = businessEnvelope.PaymentId,
                    Topic = KafkaContracts.PaymentEvents,
                    Envelope = JsonSerializer.Serialize(businessEnvelope),
                    OccurredAt = businessEnvelope.OccurredAt,
                    Published = false
                },
                cancellationToken);
        }

        var finalState = decision == ConfirmationDecision.Reject
            ? PaymentRules.InboxRejected
            : decision == ConfirmationDecision.WaitForAttempt
                ? PaymentRules.InboxFailed
                : PaymentRules.InboxProcessed;

        inbox.State = finalState;
        inbox.Attempts += 1;
        inbox.Error = finalState == PaymentRules.InboxProcessed ? string.Empty : decision.ToString();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Applied provider event {EventId} payment {PaymentId} shop {ShopId} decision {Decision}",
            envelope.EventId,
            envelope.PaymentId,
            envelope.ShopId,
            decision);
        return true;
    }
}
