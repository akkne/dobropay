namespace Dobropay.Payments.Domain;

public static class PaymentRules
{
    public const string Pending = "pending";
    public const string Succeeded = "succeeded";
    public const string Canceled = "canceled";
    public const string AttemptUnknown = "unknown";
    public const string InboxAccepted = "accepted";
    public const string InboxProcessed = "processed";
    public const string InboxRejected = "rejected";
    public const string InboxFailed = "failed";

    public static bool CanApply(string current, string next) =>
        current == Pending && (next == Succeeded || next == Canceled);
}
