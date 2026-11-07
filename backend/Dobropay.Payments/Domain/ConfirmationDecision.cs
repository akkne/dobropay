namespace Dobropay.Payments.Domain;

public enum ConfirmationDecision
{
    WaitForAttempt,
    Reject,
    Ignore,
    Apply
}
