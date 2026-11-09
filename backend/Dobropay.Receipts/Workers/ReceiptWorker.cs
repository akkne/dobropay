using System.Net.Http.Json;
using System.Text.Json;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Events;
using Dobropay.Contracts.Versioning;
using Dobropay.Messaging.Kafka;
using Dobropay.Receipts.Models;
using Dobropay.Receipts.Persistence;
using Dobropay.Receipts.Services;

namespace Dobropay.Receipts.Workers;

public sealed class ReceiptWorker(
    IServiceScopeFactory scopeFactory,
    IKafkaClientFactory kafkaClientFactory,
    ReceiptsOptions options,
    IHttpClientFactory httpClientFactory,
    ILogger<ReceiptWorker> logger) : BackgroundService
{
    private const int ReceiveTimeoutMilliseconds = 100;
    private const int CycleDelayMilliseconds = 500;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var subscriber = kafkaClientFactory.CreateSubscriber(
            KafkaContracts.ReceiptsPaymentGroup,
            KafkaContracts.PaymentEvents);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = subscriber.Receive(TimeSpan.FromMilliseconds(ReceiveTimeoutMilliseconds));
                if (message is not null)
                {
                    await ProcessEventAsync(message.RawPayload, stoppingToken);
                    subscriber.Commit(message);
                }

            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Receipt cycle failed");
            }

            await Task.Delay(CycleDelayMilliseconds, stoppingToken);
        }
    }

    private async Task ProcessEventAsync(string json, CancellationToken cancellationToken)
    {
        var envelope = JsonSerializer.Deserialize<EventEnvelope>(json)
            ?? throw new JsonException("Empty event");

        if (envelope.SchemaVersion != ContractVersions.EventSchema ||
            envelope.EventType is not (
                KafkaContracts.PaymentWaitingForCaptureType or
                KafkaContracts.PaymentSucceededType or
                KafkaContracts.PaymentCanceledType or
                KafkaContracts.RefundSucceededType or
                KafkaContracts.RefundCanceledType))
        {
            throw new JsonException("Unsupported payment event");
        }

        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IReceiptsRepository>();

        if (await repository.HasInboxEventAsync(envelope.EventId, cancellationToken))
        {
            logger.LogInformation("Receipt event {EventId} already processed", envelope.EventId);
            return;
        }

        await repository.RecordInboxEventAsync(
            envelope.EventId,
            envelope.ShopId,
            envelope.PaymentId,
            cancellationToken);

        if (envelope.EventType is not (KafkaContracts.PaymentSucceededType or KafkaContracts.RefundSucceededType))
        {
            return;
        }

        Guid? refundId = null;
        if (envelope.EventType == KafkaContracts.RefundSucceededType)
        {
            if (envelope.Payload.TryGetProperty("id", out var idProp) && idProp.TryGetGuid(out var parsedId))
            {
                refundId = parsedId;
            }
            else if (envelope.Payload.TryGetProperty("refund_id", out var refProp) && refProp.TryGetGuid(out var parsedRefId))
            {
                refundId = parsedRefId;
            }
        }

        var contextClient = scope.ServiceProvider.GetRequiredService<IPaymentReceiptContextClient>();
        var context = await contextClient.GetReceiptContextAsync(
            envelope.ShopId,
            envelope.PaymentId,
            refundId,
            cancellationToken);

        if (context is null)
        {
            logger.LogWarning(
                "Could not obtain receipt context for event {EventId} shop {ShopId} payment {PaymentId} refund {RefundId}",
                envelope.EventId,
                envelope.ShopId,
                envelope.PaymentId,
                refundId);
            return;
        }

        var receiptId = Guid.NewGuid();
        var fiscalProvider = scope.ServiceProvider.GetRequiredService<IFiscalProvider>();
        var command = new FiscalRegistrationCommand(
            receiptId,
            context.ShopId,
            context.PaymentId,
            context.RefundId,
            context.ReceiptType,
            context.Amount,
            context.Currency,
            context.TaxSystemCode,
            context.CustomerEmail,
            context.CustomerPhone,
            context.Items);

        var regResult = await fiscalProvider.RegisterReceiptAsync(command, cancellationToken);

        var fiscalReceipt = new FiscalReceipt
        {
            Id = receiptId,
            ShopId = context.ShopId,
            PaymentId = context.PaymentId,
            RefundId = context.RefundId,
            ReceiptType = context.ReceiptType,
            Status = regResult.Success ? ReceiptStatuses.Succeeded : "failed",
            TotalAmount = context.Amount,
            Currency = context.Currency,
            TaxSystemCode = context.TaxSystemCode,
            CustomerEmail = context.CustomerEmail,
            CustomerPhone = context.CustomerPhone,
            FiscalDocumentNumber = regResult.Success ? regResult.FiscalDocumentNumber : null,
            FiscalStorageNumber = regResult.Success ? regResult.FiscalStorageNumber : null,
            FiscalAttribute = regResult.Success ? regResult.FiscalAttribute : null,
            FiscalSign = regResult.Success ? regResult.FiscalSign : null,
            QrPayload = regResult.Success ? regResult.QrPayload : null,
            RegisteredAt = regResult.Success ? regResult.RegisteredAt : null,
            CreatedAt = DateTimeOffset.UtcNow,
            Items = context.Items.Select(item => new FiscalReceiptItem
            {
                Id = Guid.NewGuid(),
                ReceiptId = receiptId,
                Description = item.Description,
                Quantity = item.Quantity,
                Amount = item.Amount,
                VatCode = item.VatCode,
                PaymentMode = item.PaymentMode,
                PaymentSubject = item.PaymentSubject
            }).ToList()
        };

        await repository.SaveFiscalReceiptAsync(fiscalReceipt, cancellationToken);
        logger.LogInformation(
            "Registered fiscal receipt {ReceiptId} status {Status} FD {FiscalDocumentNumber} shop {ShopId} payment {PaymentId}",
            fiscalReceipt.Id,
            fiscalReceipt.Status,
            fiscalReceipt.FiscalDocumentNumber,
            fiscalReceipt.ShopId,
            fiscalReceipt.PaymentId);

        if (regResult.Success)
        {
            await SendFiscalReceiptEmailAsync(fiscalReceipt, cancellationToken);
        }
    }

    private async Task SendFiscalReceiptEmailAsync(FiscalReceipt receipt, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(receipt.CustomerEmail))
        {
            logger.LogWarning("Receipt {ReceiptId} has no customer email", receipt.Id);
            return;
        }

        var recipient = receipt.CustomerEmail;

        var rubles = receipt.TotalAmount / 100;
        var kopecks = Math.Abs(receipt.TotalAmount % 100);
        var typeName = receipt.ReceiptType == ReceiptTypes.Refund ? "Чек возврата прихода" : "Чек прихода";
        var itemsSummary = string.Join("; ", receipt.Items.Select(x => $"{x.Description} ({x.Quantity} шт. x {x.Amount / 100.0:F2} {receipt.Currency})"));

        var emailText = $"{typeName}. ФД №{receipt.FiscalDocumentNumber}, ФН {receipt.FiscalStorageNumber}, ФПД {receipt.FiscalAttribute}. " +
            $"Сумма: {rubles}.{kopecks:D2} {receipt.Currency}. Позиции: {itemsSummary}. QR: {receipt.QrPayload}";

        try
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add(HttpHeaderNames.InternalKey, options.InternalServiceKey);

            using var response = await client.PostAsJsonAsync(
                options.EmailServiceBaseUrl.TrimEnd('/') + InternalRouteNames.SendEmail,
                new
                {
                    ReceiptId = receipt.Id,
                    To = recipient,
                    Subject = $"Кассовый чек №{receipt.FiscalDocumentNumber} Dobropay",
                    Text = emailText
                },
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Receipt email sent for receipt {ReceiptId} to {Recipient}", receipt.Id, recipient);
            }
            else
            {
                logger.LogWarning("Email service returned {StatusCode} for receipt {ReceiptId}", response.StatusCode, receipt.Id);
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to send email for receipt {ReceiptId}", receipt.Id);
        }
    }

}
