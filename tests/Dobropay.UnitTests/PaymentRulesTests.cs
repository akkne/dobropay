using Dobropay.Contracts.Constants;
using Dobropay.Payments.Domain;

namespace Dobropay.UnitTests;

public sealed class PaymentRulesTests
{
    [Theory]
    [InlineData(PaymentRules.Pending, PaymentRules.Succeeded, true)]
    [InlineData(PaymentRules.Pending, PaymentRules.Canceled, true)]
    [InlineData(PaymentRules.Succeeded, PaymentRules.Canceled, false)]
    [InlineData(PaymentRules.Canceled, PaymentRules.Succeeded, false)]
    public void OnlyPendingCanTransition(string from, string to, bool expected) =>
        Assert.Equal(expected, PaymentRules.CanApply(from, to));

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
