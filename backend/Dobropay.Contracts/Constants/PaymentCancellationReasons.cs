namespace Dobropay.Contracts.Constants;

public static class PaymentCancellationReasons
{
    public const string CanceledByMerchant = "canceled_by_merchant";
    public const string ExpiredOnConfirmation = "expired_on_confirmation";
    public const string ExpiredOnCapture = "expired_on_capture";
    public const string ProviderDeclined = "provider_declined";
}
