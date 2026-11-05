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
}
