using Dobropay.Core;

namespace Dobropay.UnitTests;

public class PaymentRulesTests
{
    [Theory]
    [InlineData("pending", "succeeded", true)]
    [InlineData("pending", "canceled", true)]
    [InlineData("succeeded", "canceled", false)]
    [InlineData("canceled", "succeeded", false)]
    public void OnlyPendingCanTransition(string from, string to, bool expected) =>
        Assert.Equal(expected, PaymentRules.CanApply(from, to));

    [Fact]
    public void ConfirmationRejectsMismatchedAmount()
    {
        var shop = Guid.NewGuid();
        var e = new ProviderEvent("event", shop, Guid.NewGuid(), "external", 101, "RUB", "succeeded");
        var attempt = new AttemptSnapshot(shop, Guid.NewGuid(), "external", 100, "RUB", "pending");
        Assert.Equal(ConfirmationDecision.Reject, PaymentConfirmation.Decide(e, attempt));
    }
}
