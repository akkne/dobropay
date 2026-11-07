using Dobropay.Contracts.Constants;
using Dobropay.Payments.Domain;

namespace Dobropay.UnitTests;

public sealed class PaymentRulesTests
{
    [Theory]
    [InlineData(PaymentRules.Pending, PaymentRules.WaitingForCapture, true)]
    [InlineData(PaymentRules.Pending, PaymentRules.Succeeded, true)]
    [InlineData(PaymentRules.Pending, PaymentRules.Canceled, true)]
    [InlineData(PaymentRules.WaitingForCapture, PaymentRules.Succeeded, false)]
    [InlineData(PaymentRules.Succeeded, PaymentRules.Canceled, false)]
    [InlineData(PaymentRules.Canceled, PaymentRules.Succeeded, false)]
    public void OnlyPendingCanTransitionViaProviderEvent(string from, string to, bool expected) =>
        Assert.Equal(expected, PaymentRules.CanApply(from, to));

    [Theory]
    [InlineData(PaymentRules.WaitingForCapture, true)]
    [InlineData(PaymentRules.Pending, false)]
    [InlineData(PaymentRules.Succeeded, false)]
    [InlineData(PaymentRules.Canceled, false)]
    public void OnlyWaitingForCaptureCanBeCaptured(string status, bool expected) =>
        Assert.Equal(expected, PaymentRules.CanCapture(status));

    [Theory]
    [InlineData(PaymentRules.Pending, true)]
    [InlineData(PaymentRules.WaitingForCapture, true)]
    [InlineData(PaymentRules.Succeeded, false)]
    [InlineData(PaymentRules.Canceled, false)]
    public void PendingAndWaitingForCaptureCanBeCanceled(string status, bool expected) =>
        Assert.Equal(expected, PaymentRules.CanCancel(status));

    [Theory]
    [InlineData(PaymentRules.Succeeded, true, PaymentRules.Succeeded)]
    [InlineData(PaymentRules.Succeeded, false, PaymentRules.WaitingForCapture)]
    [InlineData(PaymentRules.Canceled, true, PaymentRules.Canceled)]
    [InlineData(PaymentRules.Canceled, false, PaymentRules.Canceled)]
    public void ResolveAttemptOutcomeRespectsCaptureFlag(string outcome, bool capture, string expected) =>
        Assert.Equal(expected, PaymentRules.ResolveAttemptOutcome(outcome, capture));

    [Fact]
    public void ConfirmationRejectsMismatchedAmount()
    {
        var shop = Guid.NewGuid();
        var incoming = new ProviderEvent(
            "event-1",
            shop,
            Guid.NewGuid(),
            "external-1",
            101,
            DomainConstants.DefaultCurrency,
            PaymentRules.Succeeded);
        var attempt = new AttemptSnapshot(
            shop,
            Guid.NewGuid(),
            "external-1",
            100,
            DomainConstants.DefaultCurrency,
            PaymentRules.Pending);

        Assert.Equal(ConfirmationDecision.Reject, PaymentConfirmation.Decide(incoming, attempt));
    }
}
