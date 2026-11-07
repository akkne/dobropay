using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Events;
using Dobropay.Contracts.Security;
using Dobropay.Contracts.Versioning;
using Dobropay.Messaging.Database;
using Dobropay.Payments.Domain;
using Dobropay.Payments.Persistence;
using Dobropay.Payments.Provider;
using Dobropay.Payments.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Dobropay.Payments.Api;

public static class PaymentsEndpoints
{
    private const int MaxKeyOrOrderLength = 200;
    private const int CheckoutTokenHexLength = 64;
    private const int CheckoutLifetimeMinutes = 30;
    private const int DefaultPaymentsListLimit = 100;

    public static void MapPaymentsEndpoints(this WebApplication app, PaymentsOptions options)
    {
        app.MapGet(HealthRouteNames.Live, () => Results.Ok(new { status = HealthRouteNames.StatusLive }));

        app.MapGet(HealthRouteNames.Ready, async (IDatabaseHealthChecker healthChecker, CancellationToken ct) =>
            await healthChecker.CanConnectAsync(ct)
                ? Results.Ok(new { status = HealthRouteNames.StatusReady })
                : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

        app.MapPost(PublicRouteNames.Payments, async (
            HttpContext http,
            CreatePaymentRequest body,
            IDeveloperApiClient developerApiClient,
            IPaymentsRepository repository,
            IUnitOfWork unitOfWork,
            IHashProvider hashProvider,
            ICheckoutTokenGenerator tokenGenerator,
            CancellationToken ct) =>
        {
            var shopId = await developerApiClient.ResolveShopIdByApiKeyAsync(
                http.Request.Headers[HttpHeaderNames.ApiKey].ToString(),
                ct);
            if (shopId is null)
            {
                return Results.Unauthorized();
            }

            var idempotencyKey = http.Request.Headers[HttpHeaderNames.IdempotencyKey].ToString();
            if (idempotencyKey.Length is < 1 or > MaxKeyOrOrderLength)
            {
                return Results.BadRequest(new { error = ErrorCodes.IdempotencyKeyRequired });
            }

            if (body.Amount <= 0 ||
                body.Currency != DomainConstants.DefaultCurrency ||
                body.ExternalOrderId?.Length > MaxKeyOrOrderLength)
            {
                return Results.BadRequest(new { error = ErrorCodes.InvalidPayment });
            }

            var normalizedOrderId = body.ExternalOrderId?.Trim();
            var fingerprint = hashProvider.ComputeSha256Hex(
                JsonSerializer.Serialize(new { body.Amount, body.Currency, ExternalOrderId = normalizedOrderId }));

            try
            {
                var existing = await repository.FindIdempotencyRecordAsync(
                    shopId.Value,
                    DomainConstants.CreatePaymentOperation,
                    idempotencyKey,
                    ct);
                if (existing is not null)
                {
                    if (existing.RequestHash != fingerprint)
                    {
                        return Results.Conflict(new { error = ErrorCodes.IdempotencyConflict });
                    }

                    var previousPayment = await repository.FindPaymentByIdAsync(shopId.Value, existing.PaymentId, ct);
                    return previousPayment is null
                        ? Results.NotFound()
                        : Results.Ok(BuildPaymentResponse(previousPayment, options, tokenGenerator));
                }

                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    ShopId = shopId.Value,
                    Amount = body.Amount,
                    Currency = DomainConstants.DefaultCurrency,
                    ExternalOrderId = normalizedOrderId
                };
                var idempotencyRecord = new IdempotencyRecord
                {
                    ShopId = shopId.Value,
                    Operation = DomainConstants.CreatePaymentOperation,
                    IdempotencyKey = idempotencyKey,
                    RequestHash = fingerprint,
                    PaymentId = payment.Id
                };
                var checkoutLink = new CheckoutLink
                {
                    TokenHash = hashProvider.ComputeSha256Hex(tokenGenerator.GenerateToken(payment.Id)),
                    ShopId = shopId.Value,
                    PaymentId = payment.Id,
                    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(CheckoutLifetimeMinutes)
                };

                await repository.AddPaymentAsync(payment, idempotencyRecord, checkoutLink, ct);
                await unitOfWork.SaveChangesAsync(ct);

                app.Logger.LogInformation(
                    "Payment created {PaymentId} shop {ShopId} correlation {CorrelationId}",
                    payment.Id,
                    shopId,
                    http.TraceIdentifier);
                return Results.Created(
                    $"{PublicRouteNames.Payments}/{payment.Id}",
                    BuildPaymentResponse(payment, options, tokenGenerator));
            }
            catch (DbUpdateException exception)
                when (exception.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                repository.ClearTracking();
                var existing = await repository.FindIdempotencyRecordAsync(
                    shopId.Value,
                    DomainConstants.CreatePaymentOperation,
                    idempotencyKey,
                    ct);
                if (existing is null || existing.RequestHash != fingerprint)
                {
                    return Results.Conflict(new { error = ErrorCodes.IdempotencyConflict });
                }

                var previousPayment = await repository.FindPaymentByIdAsync(shopId.Value, existing.PaymentId, ct);
                return previousPayment is null
                    ? Results.NotFound()
                    : Results.Ok(BuildPaymentResponse(previousPayment, options, tokenGenerator));
            }
        })
        .WithTags("Payments")
        .WithSummary("Create a RUB payment")
        .Produces<PaymentResponse>(StatusCodes.Status201Created)
        .Produces<PaymentResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status409Conflict);

        app.MapGet(PublicRouteNames.PaymentById, async (
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

            var payment = await repository.FindPaymentByIdAsync(shopId.Value, id, ct);
            return payment is null
                ? Results.NotFound(new { error = ErrorCodes.PaymentNotFound })
                : Results.Ok(new PaymentView(payment.Id, payment.Status, payment.Amount, payment.Currency, payment.ExternalOrderId));
        })
        .WithTags("Payments")
        .WithSummary("Get own payment")
        .Produces<PaymentView>()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound);

        app.MapGet(InternalRouteNames.ShopPaymentById, async (
            HttpContext http,
            Guid shopId,
            Guid id,
            IInternalKeyValidator keyValidator,
            IPaymentsRepository repository,
            CancellationToken ct) =>
        {
            if (!keyValidator.IsValid(http.Request.Headers[HttpHeaderNames.InternalKey]))
            {
                return Results.Unauthorized();
            }

            var payment = await repository.FindPaymentByIdAsync(shopId, id, ct);
            return payment is null
                ? Results.NotFound()
                : Results.Ok(new PaymentView(payment.Id, payment.Status, payment.Amount, payment.Currency, payment.ExternalOrderId));
        });

        app.MapGet(InternalRouteNames.ShopPayments, async (
            HttpContext http,
            Guid shopId,
            IInternalKeyValidator keyValidator,
            IPaymentsRepository repository,
            CancellationToken ct) =>
        {
            if (!keyValidator.IsValid(http.Request.Headers[HttpHeaderNames.InternalKey]))
            {
                return Results.Unauthorized();
            }

            var payments = await repository.ListShopPaymentsAsync(shopId, DefaultPaymentsListLimit, ct);
            return Results.Ok(payments);
        });

        app.MapGet(PublicRouteNames.CheckoutByToken, async (
            string token,
            IPaymentsRepository repository,
            IDeveloperApiClient developerApiClient,
            IHashProvider hashProvider,
            CancellationToken ct) =>
        {
            var link = await ResolveCheckoutLinkAsync(token, repository, hashProvider, ct);
            if (link is null)
            {
                return Results.NotFound();
            }

            if (link.ExpiresAt < DateTimeOffset.UtcNow)
            {
                return Results.StatusCode(StatusCodes.Status410Gone);
            }

            var payment = await repository.FindPaymentByIdAsync(link.ShopId, link.PaymentId, ct);
            if (payment is null)
            {
                return Results.NotFound();
            }

            var shop = await developerApiClient.GetShopAsync(link.ShopId, ct);
            var attempt = await repository.FindAttemptByPaymentAsync(link.ShopId, payment.Id, ct);

            var actions = payment.Status != PaymentRules.Pending
                ? string.Empty
                : attempt is null
                    ? $"<form method='post' action='{PublicRouteNames.CheckoutPrefix}{token}/start'><button>Перейти к оплате</button></form>"
                    : $"<form method='post' action='{PublicRouteNames.CheckoutPrefix}{token}/simulate/{PaymentRules.Succeeded}'><button>Успешно</button></form><form method='post' action='{PublicRouteNames.CheckoutPrefix}{token}/simulate/{PaymentRules.Canceled}'><button>Отменить</button></form>";

            return Results.Content(
                $"<!doctype html><html lang='ru'><meta charset='utf-8'><title>Dobropay</title><main><h1>{HtmlEncoder.Default.Encode(shop?.Name ?? "Магазин")}</h1><p>К оплате: {payment.Amount / 100m:0.00} ₽</p><p>Статус: {payment.Status}</p>{actions}</main></html>",
                DomainConstants.TextHtmlUtf8ContentType);
        });

        app.MapPost(PublicRouteNames.CheckoutStart, async (
            string token,
            IPaymentsRepository repository,
            IUnitOfWork unitOfWork,
            IPaymentProvider provider,
            IHashProvider hashProvider,
            CancellationToken ct) =>
        {
            var link = await ResolveCheckoutLinkAsync(token, repository, hashProvider, ct);
            if (link is null)
            {
                return Results.NotFound();
            }

            if (link.ExpiresAt < DateTimeOffset.UtcNow)
            {
                return Results.StatusCode(StatusCodes.Status410Gone);
            }

            var payment = await repository.FindPaymentByIdAsync(link.ShopId, link.PaymentId, ct);
            if (payment is null)
            {
                return Results.NotFound();
            }

            if (payment.Status != PaymentRules.Pending)
            {
                return Results.Redirect($"{PublicRouteNames.CheckoutPrefix}{token}");
            }

            var attempt = await repository.FindTrackedAttemptByPaymentAsync(link.ShopId, payment.Id, ct);
            if (attempt is null)
            {
                attempt = new PaymentAttempt
                {
                    Id = Guid.NewGuid(),
                    ShopId = link.ShopId,
                    PaymentId = payment.Id,
                    OperationKey = Guid.NewGuid()
                };
                await repository.AddAttemptAsync(attempt, ct);
                await unitOfWork.SaveChangesAsync(ct);
            }

            try
            {
                var externalId = await provider.CreateAttemptAsync(attempt.Id, attempt.OperationKey, ct);
                await repository.SetAttemptExternalIdAsync(attempt.Id, externalId, ct);
            }
            catch (Exception exception)
            {
                app.Logger.LogWarning(exception, "Provider outcome unknown attempt {AttemptId}", attempt.Id);
            }

            return Results.Redirect($"{PublicRouteNames.CheckoutPrefix}{token}");
        });

        if (options.SandboxEnabled)
        {
            app.MapPost(PublicRouteNames.CheckoutSimulate, async (
                string token,
                string outcome,
                IPaymentsRepository repository,
                IHashProvider hashProvider,
                IHttpClientFactory httpClientFactory,
                CancellationToken ct) =>
            {
                if (outcome is not (PaymentRules.Succeeded or PaymentRules.Canceled))
                {
                    return Results.BadRequest();
                }

                var link = await ResolveCheckoutLinkAsync(token, repository, hashProvider, ct);
                if (link is null)
                {
                    return Results.NotFound();
                }

                if (link.ExpiresAt < DateTimeOffset.UtcNow)
                {
                    return Results.StatusCode(StatusCodes.Status410Gone);
                }

                var payment = await repository.FindPaymentByIdAsync(link.ShopId, link.PaymentId, ct);
                var attempt = await repository.FindAttemptByPaymentAsync(link.ShopId, link.PaymentId, ct);
                if (payment is null || payment.Status != PaymentRules.Pending || attempt?.ExternalId is null)
                {
                    return Results.Conflict(new { error = ErrorCodes.AttemptNotReady });
                }

                var incoming = new ProviderEvent(
                    Guid.NewGuid().ToString("N"),
                    link.ShopId,
                    attempt.Id,
                    attempt.ExternalId,
                    payment.Amount,
                    payment.Currency,
                    outcome);
                var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(incoming, JsonSerializerOptions.Web);

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    options.InternalBaseUrl + PublicRouteNames.ProviderSandboxEvents)
                {
                    Content = new ByteArrayContent(payloadBytes)
                };
                request.Headers.Add(
                    HttpHeaderNames.ProviderSignature,
                    hashProvider.ComputeHmacSha256Hex(options.ProviderSecret, payloadBytes));
                request.Content.Headers.ContentType = new(DomainConstants.ApplicationJsonContentType);

                using var response = await httpClientFactory.CreateClient().SendAsync(request, ct);
                return response.IsSuccessStatusCode
                    ? Results.Redirect($"{PublicRouteNames.CheckoutPrefix}{token}")
                    : Results.StatusCode(StatusCodes.Status502BadGateway);
            });

            app.MapPost(PublicRouteNames.ProviderSandboxEvents, async (
                HttpContext http,
                IPaymentProvider provider,
                IPaymentsRepository repository,
                IUnitOfWork unitOfWork,
                CancellationToken ct) =>
            {
                using var stream = new MemoryStream();
                await http.Request.Body.CopyToAsync(stream, ct);
                var payloadBytes = stream.ToArray();

                if (!provider.VerifyEvent(payloadBytes, http.Request.Headers[HttpHeaderNames.ProviderSignature]))
                {
                    return Results.Unauthorized();
                }

                ProviderEvent? incoming;
                try
                {
                    incoming = JsonSerializer.Deserialize<ProviderEvent>(payloadBytes, JsonSerializerOptions.Web);
                }
                catch (JsonException)
                {
                    return Results.BadRequest();
                }

                if (incoming is null ||
                    incoming.ShopId == Guid.Empty ||
                    incoming.AttemptId == Guid.Empty ||
                    incoming.EventId.Length is < 1 or > MaxKeyOrOrderLength ||
                    incoming.Amount <= 0 ||
                    incoming.Currency != DomainConstants.DefaultCurrency ||
                    incoming.Outcome is not (PaymentRules.Succeeded or PaymentRules.Canceled))
                {
                    return Results.BadRequest();
                }

                var attempt = await repository.FindAttemptByIdAsync(incoming.AttemptId, ct);
                if (attempt is null)
                {
                    return Results.BadRequest(new { error = ErrorCodes.UnknownAttempt });
                }

                if (incoming.ShopId != attempt.ShopId)
                {
                    return Results.BadRequest(new { error = ErrorCodes.ShopMismatch });
                }

                var eventId = Guid.NewGuid();
                var envelope = new EventEnvelope(
                    eventId,
                    KafkaContracts.ProviderEventAcceptedType,
                    ContractVersions.EventSchema,
                    DateTimeOffset.UtcNow,
                    attempt.ShopId,
                    attempt.PaymentId,
                    http.TraceIdentifier,
                    JsonSerializer.SerializeToElement(incoming, JsonSerializerOptions.Web));

                try
                {
                    await repository.AddProviderEventAndOutboxAsync(
                        new ProviderInbox
                        {
                            Id = eventId,
                            ShopId = attempt.ShopId,
                            PaymentId = attempt.PaymentId,
                            ProviderEventId = incoming.EventId,
                            Payload = Encoding.UTF8.GetString(payloadBytes),
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
                        ct);
                    await unitOfWork.SaveChangesAsync(ct);
                }
                catch (DbUpdateException exception)
                    when (exception.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
                {
                    return Results.Accepted();
                }

                app.Logger.LogInformation(
                    "Provider event accepted {EventId} shop {ShopId} payment {PaymentId} correlation {CorrelationId}",
                    incoming.EventId,
                    attempt.ShopId,
                    attempt.PaymentId,
                    http.TraceIdentifier);
                return Results.Accepted();
            });
        }
    }

    private static PaymentResponse BuildPaymentResponse(
        Payment payment,
        PaymentsOptions options,
        ICheckoutTokenGenerator tokenGenerator) =>
        new(
            payment.Id,
            payment.Status,
            payment.Amount,
            payment.Currency,
            options.PublicBaseUrl + PublicRouteNames.CheckoutPrefix + tokenGenerator.GenerateToken(payment.Id));

    private static async Task<CheckoutLink?> ResolveCheckoutLinkAsync(
        string token,
        IPaymentsRepository repository,
        IHashProvider hashProvider,
        CancellationToken cancellationToken)
    {
        if (token.Length != CheckoutTokenHexLength)
        {
            return null;
        }

        return await repository.FindCheckoutLinkAsync(hashProvider.ComputeSha256Hex(token), cancellationToken);
    }
}
