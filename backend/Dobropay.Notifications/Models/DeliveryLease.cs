namespace Dobropay.Notifications.Models;

public sealed record DeliveryLease(
    Guid DeliveryId,
    Guid ShopId,
    string Url,
    string SigningSecret,
    string EnvelopeJson,
    int Attempts);
