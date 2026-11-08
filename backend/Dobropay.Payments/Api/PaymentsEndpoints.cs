using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Events;
using Dobropay.Contracts.Security;
using Dobropay.Contracts.Versioning;
using Dobropay.Messaging.Database;
using Dobropay.Messaging.Metrics;
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

        app.MapPost(PublicRouteNames.Payments, (
            HttpContext http,
            CreatePaymentRequest body,
            IDeveloperApiClient developerApiClient,
            IPaymentsRepository repository,
            IUnitOfWork unitOfWork,
            IHashProvider hashProvider,
            ICheckoutTokenGenerator tokenGenerator,
            IPaymentProviderRouter providerRouter,
            SbpPaymentProvider sbpProvider,
            BankCardPaymentProvider cardProvider,
            CryptoPaymentProvider cryptoProvider,
            ICryptoExchangeRateProvider cryptoRateProvider,
            IMetricsRecorder? metricsRecorder,
            CancellationToken ct) =>
            HandleCreatePaymentAsync(
                http,
                body,
                developerApiClient,
                repository,
                unitOfWork,
                hashProvider,
                tokenGenerator,
                providerRouter,
                sbpProvider,
                cardProvider,
                cryptoProvider,
                cryptoRateProvider,
                options,
                app.Logger,
                ct,
                metricsRecorder))
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
                : Results.Ok(BuildPaymentView(payment));
        })
        .WithTags("Payments")
        .WithSummary("Get own payment")
        .Produces<PaymentView>()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost(PublicRouteNames.PaymentCapture, async (
            HttpContext http,
            Guid id,
            IDeveloperApiClient developerApiClient,
            IPaymentCaptureService captureService,
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

            var body = await ReadOptionalBodyAsync<CapturePaymentRequest>(http.Request, ct) ?? new CapturePaymentRequest();
            var result = await captureService.CaptureAsync(
                shopId.Value,
                id,
                idempotencyKey,
                body,
                http.TraceIdentifier,
                ct);
            return MapOperationResult(result);
        })
        .WithTags("Payments")
        .WithSummary("Capture a waiting_for_capture payment")
        .Produces<PaymentView>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        app.MapPost(PublicRouteNames.PaymentCancel, async (
            HttpContext http,
            Guid id,
            IDeveloperApiClient developerApiClient,
            IPaymentCaptureService captureService,
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

            var body = await ReadOptionalBodyAsync<CancelPaymentRequest>(http.Request, ct) ?? new CancelPaymentRequest();
            var result = await captureService.CancelAsync(
                shopId.Value,
                id,
                idempotencyKey,
                body,
                http.TraceIdentifier,
                ct);
            return MapOperationResult(result);
        })
        .WithTags("Payments")
        .WithSummary("Cancel a pending or waiting_for_capture payment")
        .Produces<PaymentView>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

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
                : Results.Ok(BuildPaymentView(payment));
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

        app.MapPost(InternalRouteNames.ShopPaymentCapture, async (
            HttpContext http,
            Guid shopId,
            Guid id,
            IInternalKeyValidator keyValidator,
            IPaymentCaptureService captureService,
            CancellationToken ct) =>
        {
            if (!keyValidator.IsValid(http.Request.Headers[HttpHeaderNames.InternalKey]))
            {
                return Results.Unauthorized();
            }

            var idempotencyKey = ResolveInternalIdempotencyKey(http.Request);
            if (idempotencyKey is null)
            {
                return Results.BadRequest(new { error = ErrorCodes.IdempotencyKeyRequired });
            }

            var body = await ReadOptionalBodyAsync<CapturePaymentRequest>(http.Request, ct) ?? new CapturePaymentRequest();
            var result = await captureService.CaptureAsync(
                shopId,
                id,
                idempotencyKey,
                body,
                http.TraceIdentifier,
                ct);
            return MapOperationResult(result);
        });

        app.MapPost(InternalRouteNames.ShopPaymentCancel, async (
            HttpContext http,
            Guid shopId,
            Guid id,
            IInternalKeyValidator keyValidator,
            IPaymentCaptureService captureService,
            CancellationToken ct) =>
        {
            if (!keyValidator.IsValid(http.Request.Headers[HttpHeaderNames.InternalKey]))
            {
                return Results.Unauthorized();
            }

            var idempotencyKey = ResolveInternalIdempotencyKey(http.Request);
            if (idempotencyKey is null)
            {
                return Results.BadRequest(new { error = ErrorCodes.IdempotencyKeyRequired });
            }

            var body = await ReadOptionalBodyAsync<CancelPaymentRequest>(http.Request, ct) ?? new CancelPaymentRequest();
            var result = await captureService.CancelAsync(
                shopId,
                id,
                idempotencyKey,
                body,
                http.TraceIdentifier,
                ct);
            return MapOperationResult(result);
        });

        app.MapGet(InternalRouteNames.ShopPaymentReceiptContext, HandleReceiptContextAsync);

        async Task<IResult> HandleGetCheckoutSessionAsync(
            string token,
            HttpContext context,
            IPaymentsRepository repository,
            IDeveloperApiClient developerApiClient,
            IHashProvider hashProvider,
            CancellationToken ct)
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

            var acceptHeader = context.Request.Headers.Accept.ToString();
            var wantsJson = acceptHeader.Contains("application/json") ||
                            context.Request.Path.Value?.EndsWith("/session") == true ||
                            context.Request.Path.Value?.StartsWith("/v1/checkout") == true;

            if (wantsJson)
            {
                var sessionDto = new CheckoutSessionDto(
                    Token: token,
                    PaymentId: payment.Id.ToString("N"),
                    ShopId: link.ShopId.ToString("N"),
                    ShopName: shop?.Name ?? "Магазин",
                    OrderId: payment.ExternalOrderId ?? payment.Id.ToString("N"),
                    Amount: payment.Amount / 100m,
                    Currency: payment.Currency,
                    Status: payment.Status,
                    PaymentMethodType: payment.PaymentMethodType,
                    Capture: payment.Capture,
                    ExpiresAt: link.ExpiresAt,
                    CreatedAt: payment.CreatedAt,
                    Description: $"Оплата заказа {payment.ExternalOrderId ?? payment.Id.ToString("N")}",
                    ConfirmationUrl: attempt?.ConfirmationPayload ?? $"{PublicRouteNames.CheckoutPrefix}{token}/simulator?method={payment.PaymentMethodType}");

                return Results.Ok(sessionDto);
            }

            var actions = payment.Status != PaymentRules.Pending
                ? string.Empty
                : attempt is null
                    ? $"<form method='post' action='{PublicRouteNames.CheckoutPrefix}{token}/start'><button>Перейти к оплате</button></form>"
                    : $"<form method='post' action='{PublicRouteNames.CheckoutPrefix}{token}/simulate/{PaymentRules.Succeeded}'><button>Успешно</button></form><form method='post' action='{PublicRouteNames.CheckoutPrefix}{token}/simulate/{PaymentRules.Canceled}'><button>Отменить</button></form>";

            return Results.Content(
                $"<!doctype html><html lang='ru'><meta charset='utf-8'><title>Dobropay</title><main><h1>{HtmlEncoder.Default.Encode(shop?.Name ?? "Магазин")}</h1><p>К оплате: {payment.Amount / 100m:0.00} ₽</p><p>Статус: {payment.Status}</p>{actions}</main></html>",
                DomainConstants.TextHtmlUtf8ContentType);
        }

        app.MapGet(PublicRouteNames.CheckoutByToken, HandleGetCheckoutSessionAsync);
        app.MapGet(PublicRouteNames.CheckoutSession, HandleGetCheckoutSessionAsync);
        app.MapGet(PublicRouteNames.V1CheckoutByToken, HandleGetCheckoutSessionAsync);

        async Task<IResult> HandleCheckoutStartAsync(
            string token,
            HttpContext context,
            IPaymentsRepository repository,
            IUnitOfWork unitOfWork,
            IPaymentProviderRouter providerRouter,
            SbpPaymentProvider sbpProvider,
            BankCardPaymentProvider cardProvider,
            IHashProvider hashProvider,
            CancellationToken ct)
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

            var body = await ReadOptionalBodyAsync<CheckoutStartRequest>(context.Request, ct);
            if (!string.IsNullOrWhiteSpace(body?.Method))
            {
                if (body.Method == PaymentMethodTypes.Sbp || body.Method == PaymentMethodTypes.BankCard)
                {
                    payment.PaymentMethodType = body.Method;
                }
            }

            if (payment.Status != PaymentRules.Pending)
            {
                var redirectOnFinished = $"{PublicRouteNames.CheckoutPrefix}{token}";
                if (context.Request.Headers.Accept.ToString().Contains("application/json"))
                {
                    return Results.Ok(new { status = payment.Status, confirmationUrl = redirectOnFinished });
                }
                return Results.Redirect(redirectOnFinished);
            }

            var provider = providerRouter.Resolve(payment.PaymentMethodType);
            var attempt = await repository.FindTrackedAttemptByPaymentAsync(link.ShopId, payment.Id, ct);
            if (attempt is null)
            {
                attempt = new PaymentAttempt
                {
                    Id = Guid.NewGuid(),
                    ShopId = link.ShopId,
                    PaymentId = payment.Id,
                    OperationKey = Guid.NewGuid(),
                    PaymentMethodType = payment.PaymentMethodType
                };
                await repository.AddAttemptAsync(attempt, ct);
            }
            else
            {
                attempt.PaymentMethodType = payment.PaymentMethodType;
            }

            var simulatorUrl = $"{PublicRouteNames.CheckoutPrefix}{token}/simulator?method={payment.PaymentMethodType}";
            attempt.ConfirmationPayload = simulatorUrl;

            await unitOfWork.SaveChangesAsync(ct);

            try
            {
                var externalId = await provider.CreateAttemptAsync(attempt.Id, attempt.OperationKey, ct);
                await repository.SetAttemptExternalIdAsync(attempt.Id, externalId, ct);
            }
            catch (Exception exception)
            {
                app.Logger.LogWarning(exception, "Provider outcome unknown attempt {AttemptId}", attempt.Id);
            }

            if (context.Request.Headers.Accept.ToString().Contains("application/json") ||
                context.Request.ContentType?.Contains("application/json") == true)
            {
                return Results.Ok(new { status = payment.Status, confirmationUrl = simulatorUrl });
            }

            return Results.Redirect(simulatorUrl);
        }

        app.MapPost(PublicRouteNames.CheckoutStart, HandleCheckoutStartAsync);
        app.MapPost(PublicRouteNames.V1CheckoutStart, HandleCheckoutStartAsync);

        if (options.SandboxEnabled)
        {
            async Task<IResult> HandleCheckoutSimulateAsync(
                string token,
                string outcome,
                HttpContext context,
                IPaymentsRepository repository,
                IHashProvider hashProvider,
                IHttpClientFactory httpClientFactory,
                CancellationToken ct)
            {
                var effectiveOutcome = outcome == PaymentRules.WaitingForCapture ? PaymentRules.Succeeded : outcome;
                if (effectiveOutcome is not (PaymentRules.Succeeded or PaymentRules.Canceled))
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
                    effectiveOutcome);
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
                if (!response.IsSuccessStatusCode)
                {
                    return Results.StatusCode(StatusCodes.Status502BadGateway);
                }

                if (context.Request.Headers.Accept.ToString().Contains("application/json"))
                {
                    var updatedPayment = await repository.FindPaymentByIdAsync(link.ShopId, link.PaymentId, ct);
                    return Results.Ok(new { status = updatedPayment?.Status ?? effectiveOutcome });
                }

                return Results.Redirect($"{PublicRouteNames.CheckoutPrefix}{token}");
            }

            app.MapPost(PublicRouteNames.CheckoutSimulate, HandleCheckoutSimulateAsync);
            app.MapPost(PublicRouteNames.V1CheckoutSimulate, HandleCheckoutSimulateAsync);

            app.MapPost(PublicRouteNames.ProviderSandboxEvents, async (
                HttpContext http,
                SandboxProvider provider,
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
            payment.ConfirmationType == ConfirmationTypes.External
                ? null
                : options.PublicBaseUrl + PublicRouteNames.CheckoutPrefix + tokenGenerator.GenerateToken(payment.Id),
            payment.Capture,
            payment.CapturedAmount,
            payment.RefundedAmount,
            payment.PaymentMethodType,
            payment.ConfirmationType,
            payment.ConfirmationData,
            payment.SavePaymentMethod,
            payment.SavedPaymentMethodId);

    private static PaymentView BuildPaymentView(Payment payment) =>
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
            payment.SavedPaymentMethodId,
            payment.CreatedAt,
            payment.UpdatedAt);

    private static string? ResolveInternalIdempotencyKey(HttpRequest request)
    {
        var key = request.Headers[HttpHeaderNames.IdempotencyKey].ToString();
        if (string.IsNullOrWhiteSpace(key))
        {
            return Guid.NewGuid().ToString("N");
        }

        return key.Length > MaxKeyOrOrderLength ? null : key;
    }

    private static async Task<T?> ReadOptionalBodyAsync<T>(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentLength == 0)
        {
            return default;
        }

        using var stream = new MemoryStream();
        await request.Body.CopyToAsync(stream, cancellationToken);
        if (stream.Length == 0)
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(stream.ToArray(), JsonSerializerOptions.Web);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static IResult MapOperationResult(PaymentOperationResult result) =>
        result.Outcome switch
        {
            PaymentOperationOutcome.Success => Results.Ok(result.Payment),
            PaymentOperationOutcome.NotFound => Results.NotFound(new { error = result.ErrorCode ?? ErrorCodes.PaymentNotFound }),
            PaymentOperationOutcome.IdempotencyConflict => Results.Conflict(new { error = result.ErrorCode ?? ErrorCodes.IdempotencyConflict }),
            PaymentOperationOutcome.InvalidState => Results.Conflict(new { error = result.ErrorCode }),
            PaymentOperationOutcome.InvalidAmount => Results.BadRequest(new { error = result.ErrorCode }),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
        };

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

    public static async Task<IResult> HandleCreatePaymentAsync(
        HttpContext http,
        CreatePaymentRequest body,
        IDeveloperApiClient developerApiClient,
        IPaymentsRepository repository,
        IUnitOfWork unitOfWork,
        IHashProvider hashProvider,
        ICheckoutTokenGenerator tokenGenerator,
        IPaymentProviderRouter providerRouter,
        SbpPaymentProvider sbpProvider,
        BankCardPaymentProvider cardProvider,
        CryptoPaymentProvider cryptoProvider,
        ICryptoExchangeRateProvider cryptoRateProvider,
        PaymentsOptions options,
        ILogger logger,
        CancellationToken ct,
        IMetricsRecorder? metricsRecorder = null)
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

        var paymentMethodType = string.IsNullOrWhiteSpace(body.PaymentMethodType)
            ? PaymentMethodTypes.Sandbox
            : body.PaymentMethodType.Trim().ToLowerInvariant();
        if (!providerRouter.IsSupported(paymentMethodType))
        {
            return Results.BadRequest(new { error = ErrorCodes.InvalidPaymentMethod });
        }

        var confirmationType = string.IsNullOrWhiteSpace(body.ConfirmationType)
            ? ConfirmationTypes.Redirect
            : body.ConfirmationType.Trim().ToLowerInvariant();
        if (confirmationType is not (
            ConfirmationTypes.Redirect or
            ConfirmationTypes.Qr or
            ConfirmationTypes.Embedded or
            ConfirmationTypes.External))
        {
            return Results.BadRequest(new { error = ErrorCodes.InvalidConfirmationType });
        }

        if (paymentMethodType == PaymentMethodTypes.Crypto &&
            !string.IsNullOrWhiteSpace(body.CryptoNetwork) &&
            !cryptoRateProvider.IsSupportedNetwork(body.CryptoNetwork))
        {
            return Results.BadRequest(new { error = ErrorCodes.InvalidCryptoNetwork });
        }

        var normalizedOrderId = body.ExternalOrderId?.Trim();
        var fingerprint = hashProvider.ComputeSha256Hex(
            JsonSerializer.Serialize(new
            {
                body.Amount,
                body.Currency,
                ExternalOrderId = normalizedOrderId,
                body.Capture,
                PaymentMethodType = paymentMethodType,
                ConfirmationType = confirmationType,
                body.SavePaymentMethod,
                body.PaymentMethodId,
                CryptoNetwork = body.CryptoNetwork?.Trim().ToUpperInvariant(),
                Receipt = body.Receipt
            }));

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

            var now = DateTimeOffset.UtcNow;
            var expiresAt = now.AddMinutes(CheckoutLifetimeMinutes);

            if (body.PaymentMethodId is not null)
            {
                var savedMethod = await repository.FindSavedPaymentMethodAsync(
                    shopId.Value,
                    body.PaymentMethodId.Value,
                    ct);
                if (savedMethod is null)
                {
                    return Results.NotFound(new { error = ErrorCodes.PaymentMethodNotFound });
                }

                var recurringPayment = new Payment
                {
                    Id = Guid.NewGuid(),
                    ShopId = shopId.Value,
                    Amount = body.Amount,
                    Currency = DomainConstants.DefaultCurrency,
                    ExternalOrderId = normalizedOrderId,
                    Capture = body.Capture,
                    ExpiresAt = expiresAt,
                    PaymentMethodType = savedMethod.Type,
                    ConfirmationType = ConfirmationTypes.External,
                    SavePaymentMethod = body.SavePaymentMethod,
                    SavedPaymentMethodId = savedMethod.Id,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                var attemptId = Guid.NewGuid();
                var operationKey = Guid.NewGuid();
                var externalAttemptId = await cardProvider.CreateAttemptAsync(attemptId, operationKey, ct);
                var recurringAttempt = new PaymentAttempt
                {
                    Id = attemptId,
                    ShopId = shopId.Value,
                    PaymentId = recurringPayment.Id,
                    OperationKey = operationKey,
                    ExternalId = externalAttemptId,
                    State = PaymentRules.Succeeded,
                    PaymentMethodType = savedMethod.Type,
                    CreatedAt = now
                };

                var targetStatus = PaymentRules.ResolveAttemptOutcome(PaymentRules.Succeeded, body.Capture);
                recurringPayment.Status = targetStatus;
                if (targetStatus == PaymentRules.Succeeded)
                {
                    recurringPayment.CapturedAmount = recurringPayment.Amount;
                    recurringPayment.CapturedAt = now;
                }

                var recurringIdempotencyRecord = new IdempotencyRecord
                {
                    ShopId = shopId.Value,
                    Operation = DomainConstants.CreatePaymentOperation,
                    IdempotencyKey = idempotencyKey,
                    RequestHash = fingerprint,
                    PaymentId = recurringPayment.Id
                };

                var payload = JsonSerializer.SerializeToElement(new
                {
                    id = recurringPayment.Id,
                    status = targetStatus,
                    amount = recurringPayment.Amount,
                    currency = recurringPayment.Currency
                });
                var envelope = new EventEnvelope(
                    Guid.NewGuid(),
                    KafkaContracts.PaymentEventPrefix + targetStatus,
                    ContractVersions.EventSchema,
                    now,
                    shopId.Value,
                    recurringPayment.Id,
                    http.TraceIdentifier,
                    payload);
                var outboxEvent = new OutboxEvent
                {
                    Id = envelope.EventId,
                    ShopId = shopId.Value,
                    PaymentId = recurringPayment.Id,
                    Topic = KafkaContracts.PaymentEvents,
                    Envelope = JsonSerializer.Serialize(envelope),
                    OccurredAt = envelope.OccurredAt,
                    Published = false
                };

                await repository.AddPaymentAsync(recurringPayment, recurringIdempotencyRecord, null, ct);
                if (body.Receipt is not null)
                {
                    var receiptPayload = new PaymentReceiptPayload
                    {
                        Id = Guid.NewGuid(),
                        ShopId = shopId.Value,
                        PaymentId = recurringPayment.Id,
                        RefundId = null,
                        ReceiptType = ReceiptTypes.Payment,
                        TaxSystemCode = body.Receipt.TaxSystemCode ?? DomainConstants.DefaultTaxSystemCode,
                        CustomerEmail = string.IsNullOrWhiteSpace(body.Receipt.Customer?.Email) ? null : body.Receipt.Customer.Email.Trim(),
                        CustomerPhone = string.IsNullOrWhiteSpace(body.Receipt.Customer?.Phone) ? null : body.Receipt.Customer.Phone.Trim(),
                        ItemsJson = JsonSerializer.Serialize(body.Receipt.Items ?? []),
                        CreatedAt = now
                    };
                    await repository.AddReceiptPayloadAsync(receiptPayload, ct);
                }
                await repository.AddAttemptAsync(recurringAttempt, ct);
                await repository.AddOutboxEventAsync(outboxEvent, ct);
                await unitOfWork.SaveChangesAsync(ct);
                metricsRecorder?.RecordPayment(targetStatus, recurringPayment.Currency);

                logger.LogInformation(
                    "Recurring payment created {PaymentId} shop {ShopId} method {PaymentMethodId} correlation {CorrelationId}",
                    recurringPayment.Id,
                    shopId,
                    savedMethod.Id,
                    http.TraceIdentifier);
                return Results.Created(
                    $"{PublicRouteNames.Payments}/{recurringPayment.Id}",
                    BuildPaymentResponse(recurringPayment, options, tokenGenerator));
            }

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                ShopId = shopId.Value,
                Amount = body.Amount,
                Currency = DomainConstants.DefaultCurrency,
                ExternalOrderId = normalizedOrderId,
                Capture = body.Capture,
                ExpiresAt = expiresAt,
                PaymentMethodType = paymentMethodType,
                ConfirmationType = confirmationType,
                SavePaymentMethod = body.SavePaymentMethod,
                CreatedAt = now,
                UpdatedAt = now
            };

            if (body.SavePaymentMethod && body.Card is not null)
            {
                var first6 = body.Card.CardFirst6.Trim();
                var last4 = body.Card.CardLast4.Trim();
                var savedMethod = new SavedPaymentMethod
                {
                    Id = Guid.NewGuid(),
                    ShopId = shopId.Value,
                    Type = PaymentMethodTypes.BankCard,
                    CardFirst6 = first6,
                    CardLast4 = last4,
                    ExpiryMonth = body.Card.ExpiryMonth,
                    ExpiryYear = body.Card.ExpiryYear,
                    CardBrand = string.IsNullOrWhiteSpace(body.Card.CardBrand)
                        ? DomainConstants.DefaultCardBrand
                        : body.Card.CardBrand.Trim(),
                    ProviderToken = cardProvider.IssueProviderToken(payment.Id, first6, last4),
                    SavedAt = now
                };
                await repository.AddSavedPaymentMethodAsync(savedMethod, ct);
                payment.SavedPaymentMethodId = savedMethod.Id;
            }

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
                ExpiresAt = expiresAt
            };

            await repository.AddPaymentAsync(payment, idempotencyRecord, checkoutLink, ct);
            if (body.Receipt is not null)
            {
                var receiptPayload = new PaymentReceiptPayload
                {
                    Id = Guid.NewGuid(),
                    ShopId = shopId.Value,
                    PaymentId = payment.Id,
                    RefundId = null,
                    ReceiptType = ReceiptTypes.Payment,
                    TaxSystemCode = body.Receipt.TaxSystemCode ?? DomainConstants.DefaultTaxSystemCode,
                    CustomerEmail = string.IsNullOrWhiteSpace(body.Receipt.Customer?.Email) ? null : body.Receipt.Customer.Email.Trim(),
                    CustomerPhone = string.IsNullOrWhiteSpace(body.Receipt.Customer?.Phone) ? null : body.Receipt.Customer.Phone.Trim(),
                    ItemsJson = JsonSerializer.Serialize(body.Receipt.Items ?? []),
                    CreatedAt = now
                };
                await repository.AddReceiptPayloadAsync(receiptPayload, ct);
            }

            if (paymentMethodType == PaymentMethodTypes.Sbp && confirmationType == ConfirmationTypes.Qr)
            {
                var attemptId = Guid.NewGuid();
                var operationKey = Guid.NewGuid();
                var qr = sbpProvider.GenerateQrPayload(attemptId, payment.Amount, payment.Currency, expiresAt);
                var externalId = await sbpProvider.CreateAttemptAsync(attemptId, operationKey, ct);
                var sbpAttempt = new PaymentAttempt
                {
                    Id = attemptId,
                    ShopId = shopId.Value,
                    PaymentId = payment.Id,
                    OperationKey = operationKey,
                    ExternalId = externalId,
                    PaymentMethodType = PaymentMethodTypes.Sbp,
                    SbpQrcId = qr.QrcId,
                    ConfirmationPayload = qr.PayloadUrl,
                    CreatedAt = now
                };
                payment.ConfirmationData = qr.PayloadUrl;
                await repository.AddAttemptAsync(sbpAttempt, ct);
            }
            else if (paymentMethodType == PaymentMethodTypes.Crypto)
            {
                var attemptId = Guid.NewGuid();
                var operationKey = Guid.NewGuid();
                var network = string.IsNullOrWhiteSpace(body.CryptoNetwork)
                    ? CryptoNetworks.UsdtTrc20
                    : body.CryptoNetwork.Trim();
                var quote = cryptoProvider.CreateQuote(attemptId, payment.Amount, network, now);
                var externalId = await cryptoProvider.CreateAttemptAsync(attemptId, operationKey, ct);
                var cryptoAttempt = new PaymentAttempt
                {
                    Id = attemptId,
                    ShopId = shopId.Value,
                    PaymentId = payment.Id,
                    OperationKey = operationKey,
                    ExternalId = externalId,
                    PaymentMethodType = PaymentMethodTypes.Crypto,
                    CryptoCurrency = quote.CryptoCurrency,
                    CryptoNetwork = quote.Network,
                    ExchangeRate = quote.ExchangeRate,
                    CryptoAmount = quote.CryptoAmount,
                    DepositAddress = quote.DepositAddress,
                    QuoteExpiresAt = quote.QuoteExpiresAt,
                    RequiredConfirmations = quote.RequiredConfirmations,
                    Confirmations = 0,
                    ConfirmationPayload = quote.DepositAddress,
                    CreatedAt = now
                };
                payment.ConfirmationData = quote.DepositAddress;
                await repository.AddAttemptAsync(cryptoAttempt, ct);
            }

            await unitOfWork.SaveChangesAsync(ct);
            metricsRecorder?.RecordPayment(payment.Status, payment.Currency);

            logger.LogInformation(
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
    }

    public static async Task<IResult> HandleReceiptContextAsync(
        HttpContext http,
        Guid shopId,
        Guid paymentId,
        Guid? refundId,
        IInternalKeyValidator keyValidator,
        IPaymentReceiptContextService contextService,
        CancellationToken ct)
    {
        if (!keyValidator.IsValid(http.Request.Headers[HttpHeaderNames.InternalKey]))
        {
            return Results.Unauthorized();
        }

        if (refundId is null &&
            http.Request.Query.TryGetValue("refund_id", out var queryVal) &&
            Guid.TryParse(queryVal, out var parsedRefundId))
        {
            refundId = parsedRefundId;
        }

        if (refundId == Guid.Empty)
        {
            refundId = null;
        }

        var result = await contextService.GetReceiptContextAsync(shopId, paymentId, refundId, ct);
        return result.Outcome switch
        {
            PaymentReceiptContextOutcome.Success => Results.Ok(result.Context),
            PaymentReceiptContextOutcome.PaymentNotFound => Results.NotFound(new { error = result.ErrorCode ?? ErrorCodes.PaymentNotFound }),
            PaymentReceiptContextOutcome.RefundNotFound => Results.NotFound(new { error = result.ErrorCode ?? ErrorCodes.RefundNotFound }),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
