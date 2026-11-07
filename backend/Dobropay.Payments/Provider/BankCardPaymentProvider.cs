using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.Payments.Domain;

namespace Dobropay.Payments.Provider;

public sealed class BankCardPaymentProvider(
    string secret,
    string publicBaseUrl,
    IHashProvider hashProvider) : IPaymentProvider
{
    private const int TokenHashLength = 24;

    public string PaymentMethodType => PaymentMethodTypes.BankCard;

    public Task<string> CreateAttemptAsync(Guid attemptId, Guid operationKey, CancellationToken cancellationToken) =>
        Task.FromResult(DomainConstants.CardAttemptPrefix + attemptId.ToString("N"));

    public Task<string?> GetAttemptStateAsync(string externalId, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(null);

    public Task<RefundProviderResult> CreateRefundAsync(RefundProviderRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new RefundProviderResult(
            DomainConstants.CardRefundPrefix + request.RefundId.ToString("N"),
            RefundStatuses.Succeeded));

    public bool VerifyEvent(ReadOnlySpan<byte> body, string? signature) =>
        !string.IsNullOrEmpty(signature) &&
        hashProvider.FixedTimeEqualsAscii(hashProvider.ComputeHmacSha256Hex(secret, body), signature);

    public Card3dsChallenge Create3dsChallenge(Guid attemptId, string checkoutToken)
    {
        var externalAttemptId = DomainConstants.CardAttemptPrefix + attemptId.ToString("N");
        var challengeUrl = $"{publicBaseUrl}{PublicRouteNames.CheckoutPrefix}{checkoutToken}?3ds=challenge&attempt={attemptId:N}";
        var providerToken = DomainConstants.CardProviderTokenPrefix + hashProvider.ComputeSha256Hex(attemptId.ToString("N"))[..TokenHashLength];
        return new Card3dsChallenge(externalAttemptId, challengeUrl, providerToken);
    }

    public string IssueProviderToken(Guid paymentId, string cardFirst6, string cardLast4) =>
        DomainConstants.CardProviderTokenPrefix +
        hashProvider.ComputeSha256Hex($"{paymentId:N}:{cardFirst6}:{cardLast4}")[..TokenHashLength];
}
