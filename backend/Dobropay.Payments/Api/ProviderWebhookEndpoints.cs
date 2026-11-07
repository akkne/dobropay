using System.Text;
using System.Text.Json;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Events;
using Dobropay.Contracts.Versioning;
using Dobropay.Messaging.Database;
using Dobropay.Payments.Domain;
using Dobropay.Payments.Persistence;
using Dobropay.Payments.Provider;
using Dobropay.Payments.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Dobropay.Payments.Api;

public static class ProviderWebhookEndpoints
{
    private const int MaxIdLength = 200;
    private const int DefaultPaymentMethodsLimit = 100;

    public static void MapProviderWebhookEndpoints(this WebApplication app)
    {
        app.MapGet(PublicRouteNames.PaymentMethods, async (
            HttpContext http,
            IDeveloperApiClient developerApiClient,
            IPaymentsRepository repository,
            CancellationToken ct) =>
        {
            var shopId = await developerApiClient.ResolveShopIdByApiKeyAsync(
                http.Request.Headers[HttpHeaderNames.ApiKey].ToString(),
                ct);
            if (shopId is null)
            {
                return Results.Unauthorized();
            }

            var methods = await repository.ListSavedPaymentMethodsAsync(shopId.Value, DefaultPaymentMethodsLimit, ct);
            return Results.Ok(methods);
        })
        .WithTags("PaymentMethods")
        .WithSummary("List saved payment methods for a shop")
        .Produces<IReadOnlyList<SavedPaymentMethodView>>()
        .Produces(StatusCodes.Status401Unauthorized);

        app.MapGet(PublicRouteNames.PaymentMethodById, async (
            HttpContext http,
            Guid id,
            IDeveloperApiClient developerApiClient,
            IPaymentsRepository repository,
            CancellationToken ct) =>
        {
            var shopId = await developerApiClient.ResolveShopIdByApiKeyAsync(
                http.Request.Headers[HttpHeaderNames.ApiKey].ToString(),
                ct);
            if (shopId is null)
            {
                return Results.Unauthorized();
            }

            var method = await repository.FindSavedPaymentMethodViewAsync(shopId.Value, id, ct);
            return method is null
                ? Results.NotFound(new { error = ErrorCodes.PaymentMethodNotFound })
                : Results.Ok(method);
        })
        .WithTags("PaymentMethods")
        .WithSummary("Get a saved payment method by id")
        .Produces<SavedPaymentMethodView>()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost(PublicRouteNames.ProviderSbpEvents, async (
            HttpContext http,
            SbpPaymentProvider sbpProvider,
            IPaymentsRepository repository,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var payloadBytes = await ReadBodyBytesAsync(http.Request, ct);
            if (!sbpProvider.VerifyEvent(payloadBytes, http.Request.Headers[HttpHeaderNames.ProviderSignature]))
            {
                return Results.Unauthorized();
            }

            var incoming = DeserializeBody<SbpProviderEventRequest>(payloadBytes);
            if (incoming is null ||
                incoming.ShopId == Guid.Empty ||
                incoming.AttemptId == Guid.Empty ||
                string.IsNullOrWhiteSpace(incoming.EventId) ||
                incoming.EventId.Length > MaxIdLength ||
                incoming.Amount <= 0 ||
                incoming.Currency != DomainConstants.DefaultCurrency ||
                incoming.Outcome is not (PaymentRules.Succeeded or PaymentRules.Canceled) ||
                string.IsNullOrWhiteSpace(incoming.SbpOperationId) ||
                string.IsNullOrWhiteSpace(incoming.PayerBankId))
            {
                return Results.BadRequest();
            }

            var existingAttempt = await repository.FindAttemptByIdAsync(incoming.AttemptId, ct);
            if (existingAttempt is null)
            {
                return Results.BadRequest(new { error = ErrorCodes.UnknownAttempt });
            }

            if (incoming.ShopId != existingAttempt.ShopId)
            {
                return Results.BadRequest(new { error = ErrorCodes.ShopMismatch });
            }

            var trackedAttempt = await repository.FindTrackedAttemptByIdAsync(incoming.ShopId, incoming.AttemptId, ct);
            if (trackedAttempt is null)
            {
                return Results.BadRequest(new { error = ErrorCodes.UnknownAttempt });
            }

            trackedAttempt.SbpOperationId = incoming.SbpOperationId.Trim();
            trackedAttempt.PayerBankId = incoming.PayerBankId.Trim();

            var normalizedEvent = new ProviderEvent(
                incoming.EventId,
                incoming.ShopId,
                incoming.AttemptId,
                incoming.ExternalAttemptId,
                incoming.Amount,
                incoming.Currency,
                incoming.Outcome);

            return await EnqueueProviderEventAsync(
                app,
                http,
                repository,
                unitOfWork,
                trackedAttempt,
                normalizedEvent,
                payloadBytes,
                ct);
        })
        .WithTags("Providers")
        .WithSummary("Receive signed SBP provider event");

        app.MapPost(PublicRouteNames.ProviderCard3dsCallback, async (
            HttpContext http,
            BankCardPaymentProvider cardProvider,
            IPaymentsRepository repository,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var payloadBytes = await ReadBodyBytesAsync(http.Request, ct);
            if (!cardProvider.VerifyEvent(payloadBytes, http.Request.Headers[HttpHeaderNames.ProviderSignature]))
            {
                return Results.Unauthorized();
            }

            var incoming = DeserializeBody<Card3dsCallbackRequest>(payloadBytes);
            if (incoming is null ||
                incoming.ShopId == Guid.Empty ||
                incoming.AttemptId == Guid.Empty ||
                string.IsNullOrWhiteSpace(incoming.EventId) ||
                incoming.EventId.Length > MaxIdLength ||
                incoming.Amount <= 0 ||
                incoming.Currency != DomainConstants.DefaultCurrency ||
                incoming.Outcome is not (PaymentRules.Succeeded or PaymentRules.Canceled))
            {
                return Results.BadRequest();
            }

            var existingAttempt = await repository.FindAttemptByIdAsync(incoming.AttemptId, ct);
            if (existingAttempt is null)
            {
                return Results.BadRequest(new { error = ErrorCodes.UnknownAttempt });
            }

            if (incoming.ShopId != existingAttempt.ShopId)
            {
                return Results.BadRequest(new { error = ErrorCodes.ShopMismatch });
            }

            var trackedAttempt = await repository.FindTrackedAttemptByIdAsync(incoming.ShopId, incoming.AttemptId, ct);
            if (trackedAttempt is null)
            {
                return Results.BadRequest(new { error = ErrorCodes.UnknownAttempt });
            }

            var payment = await repository.FindTrackedPaymentByIdAsync(incoming.ShopId, trackedAttempt.PaymentId, ct);
            if (payment is not null &&
                payment.SavePaymentMethod &&
                payment.SavedPaymentMethodId is null &&
                incoming.Outcome == PaymentRules.Succeeded &&
                !string.IsNullOrWhiteSpace(incoming.CardFirst6) &&
                !string.IsNullOrWhiteSpace(incoming.CardLast4))
            {
                var first6 = incoming.CardFirst6.Trim();
                var last4 = incoming.CardLast4.Trim();
                var savedMethod = new SavedPaymentMethod
                {
                    Id = Guid.NewGuid(),
                    ShopId = incoming.ShopId,
                    Type = PaymentMethodTypes.BankCard,
                    CardFirst6 = first6,
                    CardLast4 = last4,
                    ExpiryMonth = incoming.ExpiryMonth ?? 12,
                    ExpiryYear = incoming.ExpiryYear ?? DateTimeOffset.UtcNow.Year + 3,
                    CardBrand = string.IsNullOrWhiteSpace(incoming.CardBrand)
                        ? DomainConstants.DefaultCardBrand
                        : incoming.CardBrand.Trim(),
                    ProviderToken = cardProvider.IssueProviderToken(payment.Id, first6, last4),
                    SavedAt = DateTimeOffset.UtcNow
                };
                await repository.AddSavedPaymentMethodAsync(savedMethod, ct);
                payment.SavedPaymentMethodId = savedMethod.Id;
            }

            var normalizedEvent = new ProviderEvent(
                incoming.EventId,
                incoming.ShopId,
                incoming.AttemptId,
                incoming.ExternalAttemptId,
                incoming.Amount,
                incoming.Currency,
                incoming.Outcome);

            return await EnqueueProviderEventAsync(
                app,
                http,
                repository,
                unitOfWork,
                trackedAttempt,
                normalizedEvent,
                payloadBytes,
                ct);
        })
        .WithTags("Providers")
        .WithSummary("Receive signed 3-D Secure callback from card provider");

        app.MapPost(PublicRouteNames.ProviderCryptoEvents, async (
            HttpContext http,
            CryptoPaymentProvider cryptoProvider,
            IPaymentsRepository repository,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var payloadBytes = await ReadBodyBytesAsync(http.Request, ct);
            if (!cryptoProvider.VerifyEvent(payloadBytes, http.Request.Headers[HttpHeaderNames.ProviderSignature]))
            {
                return Results.Unauthorized();
            }

            var incoming = DeserializeBody<CryptoProviderEventRequest>(payloadBytes);
            if (incoming is null ||
                incoming.ShopId == Guid.Empty ||
                incoming.AttemptId == Guid.Empty ||
                string.IsNullOrWhiteSpace(incoming.EventId) ||
                incoming.EventId.Length > MaxIdLength ||
                incoming.Amount <= 0 ||
                incoming.Currency != DomainConstants.DefaultCurrency ||
                incoming.Outcome is not (PaymentRules.Succeeded or PaymentRules.Canceled) ||
                string.IsNullOrWhiteSpace(incoming.TxHash) ||
                incoming.Confirmations < 0)
            {
                return Results.BadRequest();
            }

            var existingAttempt = await repository.FindAttemptByIdAsync(incoming.AttemptId, ct);
            if (existingAttempt is null)
            {
                return Results.BadRequest(new { error = ErrorCodes.UnknownAttempt });
            }

            if (incoming.ShopId != existingAttempt.ShopId)
            {
                return Results.BadRequest(new { error = ErrorCodes.ShopMismatch });
            }

            var trackedAttempt = await repository.FindTrackedAttemptByIdAsync(incoming.ShopId, incoming.AttemptId, ct);
            if (trackedAttempt is null)
            {
                return Results.BadRequest(new { error = ErrorCodes.UnknownAttempt });
            }

            trackedAttempt.TxHash = incoming.TxHash.Trim();
            trackedAttempt.Confirmations = incoming.Confirmations;

            var required = trackedAttempt.RequiredConfirmations ?? 1;
            if (incoming.Outcome == PaymentRules.Succeeded && incoming.Confirmations < required)
            {
                trackedAttempt.State = DomainConstants.CryptoConfirmingNetworkState;
                await unitOfWork.SaveChangesAsync(ct);
                return Results.Accepted(
                    value: new
                    {
                        status = DomainConstants.CryptoConfirmingNetworkState,
                        confirmations = incoming.Confirmations,
                        requiredConfirmations = required
                    });
            }

            var normalizedEvent = new ProviderEvent(
                incoming.EventId,
                incoming.ShopId,
                incoming.AttemptId,
                incoming.ExternalAttemptId,
                incoming.Amount,
                incoming.Currency,
                incoming.Outcome);

            return await EnqueueProviderEventAsync(
                app,
                http,
                repository,
                unitOfWork,
                trackedAttempt,
                normalizedEvent,
                payloadBytes,
                ct);
        })
        .WithTags("Providers")
        .WithSummary("Receive signed crypto blockchain confirmation event");
    }

    private static async Task<byte[]> ReadBodyBytesAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        await request.Body.CopyToAsync(stream, cancellationToken);
        return stream.ToArray();
    }

    private static T? DeserializeBody<T>(byte[] payloadBytes)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(payloadBytes, JsonSerializerOptions.Web);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static async Task<IResult> EnqueueProviderEventAsync(
        WebApplication app,
        HttpContext http,
        IPaymentsRepository repository,
        IUnitOfWork unitOfWork,
        PaymentAttempt attempt,
        ProviderEvent normalizedEvent,
        byte[] rawPayloadBytes,
        CancellationToken cancellationToken)
    {
        var eventId = Guid.NewGuid();
        var envelope = new EventEnvelope(
            eventId,
            KafkaContracts.ProviderEventAcceptedType,
            ContractVersions.EventSchema,
            DateTimeOffset.UtcNow,
            attempt.ShopId,
            attempt.PaymentId,
            http.TraceIdentifier,
            JsonSerializer.SerializeToElement(normalizedEvent, JsonSerializerOptions.Web));

        try
        {
            await repository.AddProviderEventAndOutboxAsync(
                new ProviderInbox
                {
                    Id = eventId,
                    ShopId = attempt.ShopId,
                    PaymentId = attempt.PaymentId,
                    ProviderEventId = normalizedEvent.EventId,
                    Payload = Encoding.UTF8.GetString(rawPayloadBytes),
                    NextAt = DateTimeOffset.UtcNow
                },
                new OutboxEvent
                {
                    Id = eventId,
                    ShopId = attempt.ShopId,
                    PaymentId = attempt.PaymentId,
                    Topic = KafkaContracts.ProviderEvents,
                    Envelope = JsonSerializer.Serialize(envelope),
                    OccurredAt = envelope.OccurredAt
                },
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Results.Accepted();
        }

        app.Logger.LogInformation(
            "Provider event accepted {EventId} shop {ShopId} payment {PaymentId} correlation {CorrelationId}",
            normalizedEvent.EventId,
            attempt.ShopId,
            attempt.PaymentId,
            http.TraceIdentifier);
        return Results.Accepted();
    }
}
