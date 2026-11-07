using System.Net.Http.Json;
using System.Text.Json;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Events;
using Dobropay.Contracts.Versioning;
using Dobropay.Messaging.Kafka;
using Dobropay.Receipts.Models;
using Dobropay.Receipts.Persistence;

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
    private const string AmountPropertyName = "amount";
    private const string CurrencyPropertyName = "currency";
    private const string DemoEmailSubject = "Демо-квитанция Dobropay";

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
                    await SaveEventAsync(message.RawPayload, stoppingToken);
                    subscriber.Commit(message);
                }

                await SendNextReceiptAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Receipt cycle failed");
            }

            await Task.Delay(CycleDelayMilliseconds, stoppingToken);
        }
    }

    private async Task SaveEventAsync(string json, CancellationToken cancellationToken)
    {
        var envelope = JsonSerializer.Deserialize<EventEnvelope>(json)
            ?? throw new JsonException("Empty event");

        if (envelope.SchemaVersion != ContractVersions.EventSchema ||
            envelope.EventType is not (KafkaContracts.PaymentSucceededType or KafkaContracts.PaymentCanceledType))
        {
            throw new JsonException("Unsupported payment event");
        }

        var isSucceeded = envelope.EventType == KafkaContracts.PaymentSucceededType;
        long amount = 0;
        var currency = DomainConstants.DefaultCurrency;
        if (isSucceeded)
        {
            amount = envelope.Payload.GetProperty(AmountPropertyName).GetInt64();
            currency = envelope.Payload.GetProperty(CurrencyPropertyName).GetString() ?? DomainConstants.DefaultCurrency;
        }

        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IReceiptsRepository>();

        await repository.SaveIncomingPaymentEventAsync(
            envelope.EventId,
            envelope.ShopId,
            envelope.PaymentId,
            isSucceeded,
            amount,
            currency,
            cancellationToken);

        logger.LogInformation(
            "Recorded receipt event {EventId} shop {ShopId} payment {PaymentId} correlation {CorrelationId}",
            envelope.EventId,
            envelope.ShopId,
            envelope.PaymentId,
            envelope.CorrelationId);
    }

    private async Task SendNextReceiptAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IReceiptsRepository>();

        var receipt = await repository.AcquireNextPendingReceiptAsync(cancellationToken);
        if (receipt is null)
        {
            return;
        }

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add(HttpHeaderNames.InternalKey, options.InternalServiceKey);

        string finalState;
        try
        {
            using var response = await client.PostAsJsonAsync(
                options.EmailServiceBaseUrl + InternalRouteNames.SendEmail,
                new
                {
                    ReceiptId = receipt.Id,
                    To = options.DemoReceiptEmail,
                    Subject = DemoEmailSubject,
                    Text = $"Демонстрационная квитанция. Платёж {receipt.PaymentId}, сумма {receipt.Amount} копеек {receipt.Currency}. Не является фискальным чеком."
                },
                cancellationToken);
            finalState = response.IsSuccessStatusCode
                ? ReceiptsSchemaConstants.EmailStateSent
                : ReceiptsSchemaConstants.EmailStateFailed;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Email outcome unknown for receipt {ReceiptId}", receipt.Id);
            return;
        }

        await repository.CompleteReceiptEmailAsync(receipt.Id, finalState, cancellationToken);
        logger.LogInformation(
            "Receipt email {ReceiptId} shop {ShopId} payment {PaymentId} state {State}",
            receipt.Id,
            receipt.ShopId,
            receipt.PaymentId,
            finalState);
    }
}
