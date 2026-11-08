using System.Text.Json;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Events;
using Dobropay.Contracts.Security;
using Dobropay.Contracts.Versioning;
using Dobropay.Messaging.Database;
using Dobropay.Messaging.Metrics;
using Dobropay.Payments.Domain;
using Dobropay.Payments.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Dobropay.Payments.Services;

public sealed class PaymentCaptureService(
    IPaymentsRepository repository,
    IUnitOfWork unitOfWork,
    IHashProvider hashProvider,
    IMetricsRecorder? metricsRecorder = null) : IPaymentCaptureService
{
    public async Task<PaymentOperationResult> CaptureAsync(
        Guid shopId,
        Guid paymentId,
        string idempotencyKey,
        CapturePaymentRequest request,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var fingerprint = hashProvider.ComputeSha256Hex(
            JsonSerializer.Serialize(new { paymentId, Amount = request.Amount }));

        var existing = await repository.FindIdempotencyRecordAsync(
            shopId,
            DomainConstants.CapturePaymentOperation,
            idempotencyKey,
            cancellationToken);
        if (existing is not null)
        {
            return await ResolveExistingIdempotencyAsync(shopId, existing, fingerprint, cancellationToken);
        }

        var payment = await repository.FindTrackedPaymentByIdAsync(shopId, paymentId, cancellationToken);
        if (payment is null)
        {
            return new PaymentOperationResult(PaymentOperationOutcome.NotFound, ErrorCode: ErrorCodes.PaymentNotFound);
        }

        if (!PaymentRules.CanCapture(payment.Status))
        {
            return new PaymentOperationResult(PaymentOperationOutcome.InvalidState, ErrorCode: ErrorCodes.InvalidCaptureState);
        }

        var captureAmount = request.Amount ?? payment.Amount;
        if (captureAmount <= 0 || captureAmount > payment.Amount)
        {
            return new PaymentOperationResult(PaymentOperationOutcome.InvalidAmount, ErrorCode: ErrorCodes.InvalidCaptureAmount);
        }

        var now = DateTimeOffset.UtcNow;
        payment.Status = PaymentRules.Succeeded;
        payment.CapturedAmount = captureAmount;
        payment.CapturedAt = now;
        payment.UpdatedAt = now;

        var idempotencyRecord = new IdempotencyRecord
        {
            ShopId = shopId,
            Operation = DomainConstants.CapturePaymentOperation,
            IdempotencyKey = idempotencyKey,
            RequestHash = fingerprint,
            PaymentId = payment.Id
        };

        var payload = JsonSerializer.SerializeToElement(new
        {
            id = payment.Id,
            status = payment.Status,
            amount = captureAmount,
            currency = payment.Currency
        });

        var envelope = new EventEnvelope(
            Guid.NewGuid(),
            KafkaContracts.PaymentSucceededType,
            ContractVersions.EventSchema,
            now,
            shopId,
            payment.Id,
            correlationId,
            payload);

        var outboxEvent = new OutboxEvent
        {
            Id = envelope.EventId,
            ShopId = shopId,
            PaymentId = payment.Id,
            Topic = KafkaContracts.PaymentEvents,
            Envelope = JsonSerializer.Serialize(envelope),
            OccurredAt = envelope.OccurredAt,
            Published = false
        };

        try
        {
            await repository.AddIdempotencyRecordAsync(idempotencyRecord, cancellationToken);
            await repository.AddOutboxEventAsync(outboxEvent, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            metricsRecorder?.RecordPayment(payment.Status, payment.Currency);
            return new PaymentOperationResult(PaymentOperationOutcome.Success, MapView(payment));
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            repository.ClearTracking();
            var concurrent = await repository.FindIdempotencyRecordAsync(
                shopId,
                DomainConstants.CapturePaymentOperation,
                idempotencyKey,
                cancellationToken);
            if (concurrent is null)
            {
                return new PaymentOperationResult(PaymentOperationOutcome.IdempotencyConflict, ErrorCode: ErrorCodes.IdempotencyConflict);
            }

            return await ResolveExistingIdempotencyAsync(shopId, concurrent, fingerprint, cancellationToken);
        }
    }

    public async Task<PaymentOperationResult> CancelAsync(
        Guid shopId,
        Guid paymentId,
        string idempotencyKey,
        CancelPaymentRequest request,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var normalizedReason = request.Reason?.Trim();
        var fingerprint = hashProvider.ComputeSha256Hex(
            JsonSerializer.Serialize(new { paymentId, Reason = normalizedReason }));

        var existing = await repository.FindIdempotencyRecordAsync(
            shopId,
            DomainConstants.CancelPaymentOperation,
            idempotencyKey,
            cancellationToken);
        if (existing is not null)
        {
            return await ResolveExistingIdempotencyAsync(shopId, existing, fingerprint, cancellationToken);
        }

        var payment = await repository.FindTrackedPaymentByIdAsync(shopId, paymentId, cancellationToken);
        if (payment is null)
        {
            return new PaymentOperationResult(PaymentOperationOutcome.NotFound, ErrorCode: ErrorCodes.PaymentNotFound);
        }

        if (!PaymentRules.CanCancel(payment.Status))
        {
            return new PaymentOperationResult(PaymentOperationOutcome.InvalidState, ErrorCode: ErrorCodes.InvalidCancelState);
        }

        var now = DateTimeOffset.UtcNow;
        var reason = string.IsNullOrWhiteSpace(normalizedReason)
            ? PaymentCancellationReasons.CanceledByMerchant
            : normalizedReason;

        if (reason.Length > PaymentsSchemaConstants.CancellationReasonMaxLength)
        {
            reason = reason[..PaymentsSchemaConstants.CancellationReasonMaxLength];
        }

        payment.Status = PaymentRules.Canceled;
        payment.CancellationParty = PaymentCancellationParties.Merchant;
        payment.CancellationReason = reason;
        payment.UpdatedAt = now;

        var idempotencyRecord = new IdempotencyRecord
        {
            ShopId = shopId,
            Operation = DomainConstants.CancelPaymentOperation,
            IdempotencyKey = idempotencyKey,
            RequestHash = fingerprint,
            PaymentId = payment.Id
        };

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
            shopId,
            payment.Id,
            correlationId,
            payload);

        var outboxEvent = new OutboxEvent
        {
            Id = envelope.EventId,
            ShopId = shopId,
            PaymentId = payment.Id,
            Topic = KafkaContracts.PaymentEvents,
            Envelope = JsonSerializer.Serialize(envelope),
            OccurredAt = envelope.OccurredAt,
            Published = false
        };

        try
        {
            await repository.AddIdempotencyRecordAsync(idempotencyRecord, cancellationToken);
            await repository.AddOutboxEventAsync(outboxEvent, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            metricsRecorder?.RecordPayment(payment.Status, payment.Currency);
            return new PaymentOperationResult(PaymentOperationOutcome.Success, MapView(payment));
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            repository.ClearTracking();
            var concurrent = await repository.FindIdempotencyRecordAsync(
                shopId,
                DomainConstants.CancelPaymentOperation,
                idempotencyKey,
                cancellationToken);
            if (concurrent is null)
            {
                return new PaymentOperationResult(PaymentOperationOutcome.IdempotencyConflict, ErrorCode: ErrorCodes.IdempotencyConflict);
            }

            return await ResolveExistingIdempotencyAsync(shopId, concurrent, fingerprint, cancellationToken);
        }
    }

    private async Task<PaymentOperationResult> ResolveExistingIdempotencyAsync(
        Guid shopId,
        IdempotencyRecord existing,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        if (existing.RequestHash != fingerprint)
        {
            return new PaymentOperationResult(
                PaymentOperationOutcome.IdempotencyConflict,
                ErrorCode: ErrorCodes.IdempotencyConflict);
        }

        var payment = await repository.FindPaymentByIdAsync(shopId, existing.PaymentId, cancellationToken);
        return payment is null
            ? new PaymentOperationResult(PaymentOperationOutcome.NotFound, ErrorCode: ErrorCodes.PaymentNotFound)
            : new PaymentOperationResult(PaymentOperationOutcome.Success, MapView(payment));
    }

    private static PaymentView MapView(Payment payment) =>
        new(
            payment.Id,
            payment.Status,
            payment.Amount,
            payment.Currency,
            payment.ExternalOrderId,
            payment.Capture,
            payment.CapturedAmount,
            payment.RefundedAmount,
            payment.CancellationParty,
            payment.CancellationReason,
            payment.PaymentMethodType,
            payment.ConfirmationType,
            payment.ConfirmationData,
            payment.SavePaymentMethod,
            payment.SavedPaymentMethodId);
}
