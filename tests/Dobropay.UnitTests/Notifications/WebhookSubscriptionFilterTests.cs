using System.Text.Json;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Events;
using Dobropay.Notifications.Persistence;

namespace Dobropay.UnitTests.Notifications;

public sealed class WebhookSubscriptionFilterTests
{
    [Fact]
    public void MatchesEventType_ReturnsTrue_WhenEventTypesIsNull()
    {
        var subscription = new WebhookSubscription
        {
            EventTypes = null
        };

        Assert.True(subscription.MatchesEventType(KafkaContracts.PaymentSucceededType));
        Assert.True(subscription.MatchesEventType(KafkaContracts.PaymentCanceledType));
        Assert.True(subscription.MatchesEventType(KafkaContracts.ReceiptSucceededType));
    }

    [Fact]
    public void MatchesEventType_ReturnsTrue_WhenEventTypesIsEmptyString()
    {
        var subscription = new WebhookSubscription
        {
            EventTypes = "  "
        };

        Assert.True(subscription.MatchesEventType(KafkaContracts.PaymentSucceededType));
        Assert.True(subscription.MatchesEventType(KafkaContracts.RefundSucceededType));
    }

    [Fact]
    public void MatchesEventType_FiltersCorrectly_WithCommaSeparatedTypes()
    {
        var subscription = new WebhookSubscription
        {
            EventTypes = $"{KafkaContracts.PaymentSucceededType}, {KafkaContracts.RefundSucceededType}"
        };

        Assert.True(subscription.MatchesEventType(KafkaContracts.PaymentSucceededType));
        Assert.True(subscription.MatchesEventType(KafkaContracts.RefundSucceededType));
        Assert.False(subscription.MatchesEventType(KafkaContracts.PaymentCanceledType));
        Assert.False(subscription.MatchesEventType(KafkaContracts.PaymentWaitingForCaptureType));
    }

    [Fact]
    public void MatchesEventType_FiltersCorrectly_WithJsonArray()
    {
        var subscription = new WebhookSubscription
        {
            EventTypes = $"[\"{KafkaContracts.PaymentSucceededType}\"]"
        };

        Assert.True(subscription.MatchesEventType(KafkaContracts.PaymentSucceededType));
        Assert.False(subscription.MatchesEventType(KafkaContracts.PaymentCanceledType));
    }

    [Fact]
    public async Task SaveIncomingPaymentEventAsync_CreatesDelivery_OnlyForSubscribedEventTypes()
    {
        var repository = new FakeNotificationsRepository();
        var shopId = Guid.NewGuid();

        var subFiltered = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Url = "https://example.com/succeeded",
            SigningSecret = "secret-1",
            EventTypes = KafkaContracts.PaymentSucceededType
        };

        var subAll = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Url = "https://example.com/all",
            SigningSecret = "secret-2",
            EventTypes = null
        };

        var subCanceled = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Url = "https://example.com/canceled",
            SigningSecret = "secret-3",
            EventTypes = KafkaContracts.PaymentCanceledType
        };

        repository.Subscriptions.AddRange([subFiltered, subAll, subCanceled]);

        var paymentId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var envelope = new EventEnvelope(
            eventId,
            KafkaContracts.PaymentSucceededType,
            1,
            DateTimeOffset.UtcNow,
            shopId,
            paymentId,
            "corr-1",
            JsonSerializer.SerializeToElement(new { id = paymentId }));

        var rawEnvelope = JsonSerializer.Serialize(envelope);

        await repository.SaveIncomingPaymentEventAsync(
            eventId,
            shopId,
            paymentId,
            rawEnvelope,
            CancellationToken.None);

        Assert.Equal(2, repository.Deliveries.Count);
        Assert.Contains(repository.Deliveries, d => d.SubscriptionId == subFiltered.Id);
        Assert.Contains(repository.Deliveries, d => d.SubscriptionId == subAll.Id);
        Assert.DoesNotContain(repository.Deliveries, d => d.SubscriptionId == subCanceled.Id);
    }
}
