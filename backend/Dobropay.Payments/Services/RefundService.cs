using System.Text.Json;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Events;
using Dobropay.Contracts.Security;
using Dobropay.Contracts.Versioning;
using Dobropay.Messaging.Database;
using Dobropay.Payments.Domain;
using Dobropay.Payments.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Dobropay.Payments.Services;

public sealed class RefundService(
    IPaymentsRepository paymentsRepository,
    IRefundRepository refundRepository,
    IPaymentProviderRouter providerRouter,
    IUnitOfWork unitOfWork,
    IHashProvider hashProvider) : IRefundService
{
    public async Task<RefundOperationResult> CreateRefundAsync(
        Guid shopId,
        string idempotencyKey,
        CreateRefundRequest request,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (request.PaymentId == Guid.Empty ||
            request.Amount <= 0 ||
            request.Currency != DomainConstants.DefaultCurrency ||
            (request.Reason?.Length ?? 0) > PaymentsSchemaConstants.RefundReasonMaxLength)
        {
            return new RefundOperationResult(
                RefundOperationOutcome.InvalidAmount,
                ErrorCode: ErrorCodes.InvalidRefund);
        }

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        var fingerprint = hashProvider.ComputeSha256Hex(
            JsonSerializer.Serialize(new
            {
                request.PaymentId,
                request.Amount,
                request.Currency,
                Reason = reason,
                Receipt = request.Receipt
            }));

        var existing = await paymentsRepository.FindIdempotencyRecordAsync(
            shopId,
            DomainConstants.CreateRefundOperation,
            idempotencyKey,
            cancellationToken);
        if (existing is not null)
        {
            return await ResolveExistingIdempotencyAsync(shopId, existing, fingerprint, cancellationToken);
        }

        var payment = await paymentsRepository.FindTrackedPaymentByIdAsync(shopId, request.PaymentId, cancellationToken);
        if (payment is null)
        {
            return new RefundOperationResult(
                RefundOperationOutcome.PaymentNotFound,
                ErrorCode: ErrorCodes.PaymentNotFound);
        }

        if (payment.Status != PaymentRules.Succeeded)
        {
            return new RefundOperationResult(
                RefundOperationOutcome.InvalidState,
                ErrorCode: ErrorCodes.InvalidRefund);
        }

        var capturedAmount = payment.CapturedAmount ?? payment.Amount;
        if (!PaymentRules.CanRefund(payment.Status, capturedAmount, payment.RefundedAmount, request.Amount))
        {
            return new RefundOperationResult(
                RefundOperationOutcome.AmountExceeded,
                ErrorCode: ErrorCodes.RefundAmountExceeded);
        }

        var refundId = Guid.NewGuid();
        var provider = providerRouter.Resolve(payment.PaymentMethodType);
        var providerResult = await provider.CreateRefundAsync(
            new RefundProviderRequest(refundId, payment.Id, request.Amount, request.Currency),
            cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var refund = new Refund
        {
            Id = refundId,
            ShopId = shopId,
            PaymentId = payment.Id,
            Amount = request.Amount,
            Currency = DomainConstants.DefaultCurrency,
            Status = providerResult.Status,
            Reason = reason,
            ExternalRefundId = providerResult.ExternalRefundId,
            CreatedAt = now,
            CompletedAt = providerResult.Status == RefundStatuses.Succeeded ? now : null
        };

        if (providerResult.Status == RefundStatuses.Succeeded)
        {
            payment.RefundedAmount += request.Amount;
            payment.UpdatedAt = now;
        }

        var idempotencyRecord = new IdempotencyRecord
        {
            ShopId = shopId,
            Operation = DomainConstants.CreateRefundOperation,
            IdempotencyKey = idempotencyKey,
            RequestHash = fingerprint,
            PaymentId = payment.Id,
            ResourceId = refund.Id
        };

        var eventType = providerResult.Status == RefundStatuses.Succeeded
            ? KafkaContracts.RefundSucceededType
            : KafkaContracts.RefundCanceledType;

        var payload = JsonSerializer.SerializeToElement(new
        {
            id = refund.Id,
            payment_id = payment.Id,
            status = refund.Status,
            amount = refund.Amount,
            currency = refund.Currency,
            reason = refund.Reason
        });

        var envelope = new EventEnvelope(
            Guid.NewGuid(),
            eventType,
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
            await refundRepository.AddRefundAsync(refund, cancellationToken);
            if (request.Receipt is not null)
            {
                var receiptPayload = new PaymentReceiptPayload
                {
                    Id = Guid.NewGuid(),
                    ShopId = shopId,
                    PaymentId = payment.Id,
                    RefundId = refund.Id,
                    ReceiptType = ReceiptTypes.Refund,
                    TaxSystemCode = request.Receipt.TaxSystemCode ?? DomainConstants.DefaultTaxSystemCode,
                    CustomerEmail = string.IsNullOrWhiteSpace(request.Receipt.Customer?.Email) ? null : request.Receipt.Customer.Email.Trim(),
                    CustomerPhone = string.IsNullOrWhiteSpace(request.Receipt.Customer?.Phone) ? null : request.Receipt.Customer.Phone.Trim(),
                    ItemsJson = JsonSerializer.Serialize(request.Receipt.Items ?? []),
                    CreatedAt = now
                };
                await paymentsRepository.AddReceiptPayloadAsync(receiptPayload, cancellationToken);
            }
            await paymentsRepository.AddIdempotencyRecordAsync(idempotencyRecord, cancellationToken);
            await paymentsRepository.AddOutboxEventAsync(outboxEvent, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new RefundOperationResult(RefundOperationOutcome.Success, MapView(refund), Created: true);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            paymentsRepository.ClearTracking();
            var concurrent = await paymentsRepository.FindIdempotencyRecordAsync(
                shopId,
                DomainConstants.CreateRefundOperation,
                idempotencyKey,
                cancellationToken);
            if (concurrent is null)
            {
                return new RefundOperationResult(
                    RefundOperationOutcome.IdempotencyConflict,
                    ErrorCode: ErrorCodes.IdempotencyConflict);
            }

            return await ResolveExistingIdempotencyAsync(shopId, concurrent, fingerprint, cancellationToken);
        }
    }

    private async Task<RefundOperationResult> ResolveExistingIdempotencyAsync(
        Guid shopId,
        IdempotencyRecord existing,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        if (existing.RequestHash != fingerprint)
        {
            return new RefundOperationResult(
                RefundOperationOutcome.IdempotencyConflict,
                ErrorCode: ErrorCodes.IdempotencyConflict);
        }

        var refund = await refundRepository.FindByIdAsync(shopId, existing.ResourceId ?? Guid.Empty, cancellationToken);
        return refund is null
            ? new RefundOperationResult(RefundOperationOutcome.RefundNotFound, ErrorCode: ErrorCodes.RefundNotFound)
            : new RefundOperationResult(RefundOperationOutcome.Success, MapView(refund));
    }

    private static RefundView MapView(Refund refund) =>
        new(
            refund.Id,
            refund.PaymentId,
            refund.Status,
            refund.Amount,
            refund.Currency,
            refund.Reason,
            refund.ExternalRefundId,
            refund.CreatedAt,
            refund.CompletedAt);
}
