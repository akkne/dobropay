namespace Dobropay.Contracts.Constants;

public static class PaymentStatuses
{
    public const string Pending = "pending";
    public const string WaitingForCapture = "waiting_for_capture";
    public const string Succeeded = "succeeded";
    public const string Canceled = "canceled";
}
