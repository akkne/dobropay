namespace Dobropay.Payments.Domain;

public static class PaymentConfirmation
{
    public static ConfirmationDecision Decide(ProviderEvent incoming, AttemptSnapshot? attempt)
    {
        if (attempt is null || attempt.ExternalId is null)
        {
            return ConfirmationDecision.WaitForAttempt;
        }

        if (incoming.Outcome is not (PaymentRules.Succeeded or PaymentRules.Canceled) ||
            incoming.ShopId != attempt.ShopId ||
            incoming.ExternalAttemptId != attempt.ExternalId ||
            incoming.Amount != attempt.Amount ||
            incoming.Currency != attempt.Currency)
        {
            return ConfirmationDecision.Reject;
        }

        return PaymentRules.CanApply(attempt.PaymentStatus, incoming.Outcome)
            ? ConfirmationDecision.Apply
            : ConfirmationDecision.Ignore;
    }
}
