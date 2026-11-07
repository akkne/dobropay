using Dobropay.Contracts.Constants;

namespace Dobropay.Payments.Domain;

public static class PaymentRules
{
    public const string Pending = PaymentStatuses.Pending;
    public const string WaitingForCapture = PaymentStatuses.WaitingForCapture;
    public const string Succeeded = PaymentStatuses.Succeeded;
    public const string Canceled = PaymentStatuses.Canceled;
    public const string AttemptUnknown = "unknown";
    public const string InboxAccepted = "accepted";
    public const string InboxProcessed = "processed";
    public const string InboxRejected = "rejected";
    public const string InboxFailed = "failed";

    public static bool CanApply(string current, string next) =>
        current == Pending && (next == WaitingForCapture || next == Succeeded || next == Canceled);

    public static bool CanCapture(string status) =>
        status == WaitingForCapture;

    public static bool CanCancel(string status) =>
        status is Pending or WaitingForCapture;

    public static string ResolveAttemptOutcome(string providerOutcome, bool capture) =>
        providerOutcome == Succeeded
            ? (capture ? Succeeded : WaitingForCapture)
            : providerOutcome;
}
