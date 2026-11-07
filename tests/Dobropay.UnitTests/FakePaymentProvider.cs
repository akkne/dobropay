using Dobropay.Contracts.Constants;
using Dobropay.Payments.Domain;

namespace Dobropay.UnitTests;

public sealed class FakePaymentProvider : IPaymentProvider
{
    private readonly Dictionary<string, string?> _attemptStates = [];

    public string PaymentMethodType => PaymentMethodTypes.Sandbox;

    public string NextRefundStatus { get; set; } = RefundStatuses.Succeeded;

    public void SetAttemptState(string externalId, string? state) =>
        _attemptStates[externalId] = state;

    public Task<string> CreateAttemptAsync(Guid attemptId, Guid operationKey, CancellationToken cancellationToken) =>
        Task.FromResult(DomainConstants.SimulatorAttemptPrefix + attemptId.ToString("N"));

    public Task<string?> GetAttemptStateAsync(string externalId, CancellationToken cancellationToken) =>
        Task.FromResult(_attemptStates.GetValueOrDefault(externalId));

    public Task<RefundProviderResult> CreateRefundAsync(RefundProviderRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new RefundProviderResult(
            DomainConstants.SimulatorRefundPrefix + request.RefundId.ToString("N"),
            NextRefundStatus));

    public bool VerifyEvent(ReadOnlySpan<byte> body, string? signature) => true;
}
