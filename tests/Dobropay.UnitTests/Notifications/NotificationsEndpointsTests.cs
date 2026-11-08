using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Events;
using Dobropay.Contracts.Security;
using Dobropay.Notifications.Api;
using Dobropay.Notifications.Models;
using Dobropay.Notifications.Persistence;
using Dobropay.Notifications.Webhooks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Dobropay.UnitTests.Notifications;

public sealed class NotificationsEndpointsTests
{
    private const string ExpectedInternalKey = "test-internal-key";

    private readonly IInternalKeyValidator _keyValidator =
        new InternalKeyValidator(ExpectedInternalKey, new Sha256HashProvider());

    private static IWebhookUrlValidator CreateUrlValidator(bool sandboxEnabled = false) =>
        new WebhookUrlValidator(new NotificationsOptions
        {
            SandboxEnabled = sandboxEnabled,
            WebhookSecret = "test-secret"
        });

    [Fact]
    public async Task HandleSaveSubscriptionAsync_ReturnsUnauthorized_WhenKeyIsInvalid()
    {
        var repository = new FakeNotificationsRepository();
        var validator = CreateUrlValidator();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[HttpHeaderNames.InternalKey] = "invalid-key";

        var request = new ConfigureWebhookSubscriptionRequest
        {
            Url = "https://8.8.8.8/webhook",
            SigningSecret = "secret"
        };

        var result = await NotificationsEndpoints.HandleSaveSubscriptionAsync(
            httpContext,
            Guid.NewGuid(),
            request,
            _keyValidator,
            validator,
            repository,
            CancellationToken.None);

        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task HandleSaveSubscriptionAsync_ReturnsBadRequest_WhenSsrfValidationFails()
    {
        var repository = new FakeNotificationsRepository();
        var validator = CreateUrlValidator(sandboxEnabled: false);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[HttpHeaderNames.InternalKey] = ExpectedInternalKey;

        var request = new ConfigureWebhookSubscriptionRequest
        {
            Url = "https://10.0.0.1/webhook",
            SigningSecret = "secret"
        };

        var result = await NotificationsEndpoints.HandleSaveSubscriptionAsync(
            httpContext,
            Guid.NewGuid(),
            request,
            _keyValidator,
            validator,
            repository,
            CancellationToken.None);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }

    [Fact]
    public async Task HandleSaveSubscriptionAsync_ReturnsOk_AndSavesSubscriptionWithEventTypes()
    {
        var repository = new FakeNotificationsRepository();
        var validator = CreateUrlValidator(sandboxEnabled: false);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[HttpHeaderNames.InternalKey] = ExpectedInternalKey;
        var shopId = Guid.NewGuid();

        var request = new ConfigureWebhookSubscriptionRequest
        {
            Url = "https://8.8.8.8/webhook",
            SigningSecret = "my-secret",
            EventTypes = [KafkaContracts.PaymentSucceededType, KafkaContracts.RefundSucceededType]
        };

        var result = await NotificationsEndpoints.HandleSaveSubscriptionAsync(
            httpContext,
            shopId,
            request,
            _keyValidator,
            validator,
            repository,
            CancellationToken.None);

        var okResult = Assert.IsType<Ok<WebhookSubscriptionResponse>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal(shopId, okResult.Value.ShopId);
        Assert.Equal("https://8.8.8.8/webhook", okResult.Value.Url);
        Assert.Equal("my-secret", okResult.Value.SigningSecret);
        Assert.Equal(2, okResult.Value.EventTypes?.Count);
        Assert.Contains(KafkaContracts.PaymentSucceededType, okResult.Value.EventTypes!);

        var saved = Assert.Single(repository.Subscriptions);
        Assert.Equal(shopId, saved.ShopId);
        Assert.Equal("payment.succeeded,refund.succeeded", saved.EventTypes);
    }

    [Fact]
    public async Task HandleGetSubscriptionAsync_ReturnsNotFound_WhenDoesNotExist()
    {
        var repository = new FakeNotificationsRepository();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[HttpHeaderNames.InternalKey] = ExpectedInternalKey;

        var result = await NotificationsEndpoints.HandleGetSubscriptionAsync(
            httpContext,
            Guid.NewGuid(),
            _keyValidator,
            repository,
            CancellationToken.None);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task HandleGetSubscriptionAsync_ReturnsOk_WhenExists()
    {
        var repository = new FakeNotificationsRepository();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[HttpHeaderNames.InternalKey] = ExpectedInternalKey;
        var shopId = Guid.NewGuid();

        await repository.SaveSubscriptionAsync(
            shopId,
            "https://8.8.8.8/webhook",
            "sec",
            [KafkaContracts.PaymentSucceededType],
            CancellationToken.None);

        var result = await NotificationsEndpoints.HandleGetSubscriptionAsync(
            httpContext,
            shopId,
            _keyValidator,
            repository,
            CancellationToken.None);

        var okResult = Assert.IsType<Ok<WebhookSubscriptionResponse>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal(shopId, okResult.Value.ShopId);
        Assert.Equal("https://8.8.8.8/webhook", okResult.Value.Url);
        Assert.Equal("sec", okResult.Value.SigningSecret);
        Assert.Equal([KafkaContracts.PaymentSucceededType], okResult.Value.EventTypes);
    }

    [Fact]
    public async Task HandleRetryDeliveryAsync_ReturnsUnauthorized_WhenKeyIsInvalid()
    {
        var repository = new FakeNotificationsRepository();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[HttpHeaderNames.InternalKey] = "wrong-key";

        var result = await NotificationsEndpoints.HandleRetryDeliveryAsync(
            httpContext,
            Guid.NewGuid(),
            Guid.NewGuid(),
            _keyValidator,
            repository,
            CancellationToken.None);

        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task HandleRetryDeliveryAsync_ReturnsNotFound_WhenDeliveryDoesNotExist()
    {
        var repository = new FakeNotificationsRepository();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[HttpHeaderNames.InternalKey] = ExpectedInternalKey;

        var result = await NotificationsEndpoints.HandleRetryDeliveryAsync(
            httpContext,
            Guid.NewGuid(),
            Guid.NewGuid(),
            _keyValidator,
            repository,
            CancellationToken.None);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task HandleRetryDeliveryAsync_ResetsStateAndReturnsOk_WhenDeliveryExists()
    {
        var repository = new FakeNotificationsRepository();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[HttpHeaderNames.InternalKey] = ExpectedInternalKey;
        var shopId = Guid.NewGuid();
        var deliveryId = Guid.NewGuid();

        repository.Deliveries.Add(new WebhookDelivery
        {
            Id = deliveryId,
            ShopId = shopId,
            EventId = Guid.NewGuid(),
            SubscriptionId = Guid.NewGuid(),
            State = NotificationsSchemaConstants.StateFailed,
            Attempts = 5,
            NextAt = DateTimeOffset.UtcNow.AddHours(-1),
            LockedUntil = DateTimeOffset.UtcNow.AddMinutes(10),
            LastResult = NotificationsSchemaConstants.ResultHttpError
        });

        var result = await NotificationsEndpoints.HandleRetryDeliveryAsync(
            httpContext,
            shopId,
            deliveryId,
            _keyValidator,
            repository,
            CancellationToken.None);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);
        var delivery = Assert.Single(repository.Deliveries);
        Assert.Equal(NotificationsSchemaConstants.StatePending, delivery.State);
        Assert.Equal(0, delivery.Attempts);
        Assert.Null(delivery.LockedUntil);
        Assert.True(delivery.NextAt <= DateTimeOffset.UtcNow);
    }
}
