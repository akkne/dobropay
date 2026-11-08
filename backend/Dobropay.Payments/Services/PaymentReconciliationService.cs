using System.Text.Json;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Events;
using Dobropay.Contracts.Versioning;
using Dobropay.Messaging.Database;
using Dobropay.Messaging.Metrics;
using Dobropay.Payments.Domain;
using Dobropay.Payments.Persistence;

namespace Dobropay.Payments.Services;

public sealed class PaymentReconciliationService(
    IPaymentsRepository repository,
    IPaymentProviderRouter providerRouter,
    IUnitOfWork unitOfWork,
    ReconciliationOptions options,
    IMetricsRecorder? metricsRecorder = null) : IPaymentReconciliationService
{
    private const int BatchLimit = 50;

    public async Task<int> ReconcileStaleAttemptsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var cutoff = now.AddSeconds(-options.AttemptStaleSeconds);
        var staleAttempts = await repository.ListStaleUnknownAttemptsAsync(cutoff, BatchLimit, cancellationToken);
        var reconciledCount = 0;

        foreach (var attempt in staleAttempts)
        {
            if (attempt.ExternalId is null)
            {
                continue;
            }

            var provider = providerRouter.Resolve(attempt.PaymentMethodType);
            var providerOutcome = await provider.GetAttemptStateAsync(attempt.ExternalId, cancellationToken);
            if (providerOutcome is not (PaymentRules.Succeeded or PaymentRules.Canceled))
            {
                continue;
            }

            attempt.State = providerOutcome;
            var payment = await repository.FindTrackedPaymentByIdAsync(attempt.ShopId, attempt.PaymentId, cancellationToken);
            if (payment is not null)
            {
                var targetStatus = PaymentRules.ResolveAttemptOutcome(providerOutcome, payment.Capture);
                if (PaymentRules.CanApply(payment.Status, targetStatus))
                {
                    payment.Status = targetStatus;
                    payment.UpdatedAt = now;

                    if (targetStatus == PaymentRules.Succeeded)
                    {
                        payment.CapturedAmount = payment.Amount;
                        payment.CapturedAt = now;
                    }
                    else if (targetStatus == PaymentRules.WaitingForCapture)
                    {
                        payment.ExpiresAt = now.AddMinutes(options.CaptureExpirationMinutes);
                    }
                    else if (targetStatus == PaymentRules.Canceled)
                    {
                        payment.CancellationParty = PaymentCancellationParties.PaymentNetwork;
                        payment.CancellationReason = PaymentCancellationReasons.ProviderDeclined;
                    }

                    var payload = JsonSerializer.SerializeToElement(new
                    {
                        id = payment.Id,
                        status = targetStatus,
                        amount = payment.Amount,
                        currency = payment.Currency
                    });

                    var envelope = new EventEnvelope(
                        Guid.NewGuid(),
                        KafkaContracts.PaymentEventPrefix + targetStatus,
                        ContractVersions.EventSchema,
                        now,
                        payment.ShopId,
                        payment.Id,
                        Guid.NewGuid().ToString("N"),
                        payload);

                    await repository.AddOutboxEventAsync(
                        new OutboxEvent
                        {
                            Id = envelope.EventId,
                            ShopId = payment.ShopId,
                            PaymentId = payment.Id,
                            Topic = KafkaContracts.PaymentEvents,
                            Envelope = JsonSerializer.Serialize(envelope),
                            OccurredAt = envelope.OccurredAt,
                            Published = false
                        },
                        cancellationToken);

                    metricsRecorder?.RecordPayment(targetStatus, payment.Currency);
                }
            }

            reconciledCount += 1;
        }

        if (reconciledCount > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return reconciledCount;
    }

    public async Task<int> CancelExpiredPaymentsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var expiredPayments = await repository.ListExpiredActivePaymentsAsync(now, BatchLimit, cancellationToken);
        foreach (var payment in expiredPayments)
        {
            var reason = payment.Status == PaymentRules.WaitingForCapture
                ? PaymentCancellationReasons.ExpiredOnCapture
                : PaymentCancellationReasons.ExpiredOnConfirmation;

            payment.Status = PaymentRules.Canceled;
            payment.CancellationParty = PaymentCancellationParties.Dobropay;
            payment.CancellationReason = reason;
            payment.UpdatedAt = now;

            var payload = JsonSerializer.SerializeToElement(new
            {
                id = payment.Id,
                status = payment.Status,
                amount = payment.Amount,
                currency = payment.Currency,
                cancellation_party = payment.CancellationParty,
                cancellation_reason = payment.CancellationReason
            });

            var envelope = new EventEnvelope(
                Guid.NewGuid(),
                KafkaContracts.PaymentCanceledType,
                ContractVersions.EventSchema,
                now,
                payment.ShopId,
                payment.Id,
                Guid.NewGuid().ToString("N"),
                payload);

            await repository.AddOutboxEventAsync(
                new OutboxEvent
                {
                    Id = envelope.EventId,
                    ShopId = payment.ShopId,
                    PaymentId = payment.Id,
                    Topic = KafkaContracts.PaymentEvents,
                    Envelope = JsonSerializer.Serialize(envelope),
                    OccurredAt = envelope.OccurredAt,
                    Published = false
                },
                cancellationToken);

            metricsRecorder?.RecordPayment(payment.Status, payment.Currency);
        }

        if (expiredPayments.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return expiredPayments.Count;
    }
}
