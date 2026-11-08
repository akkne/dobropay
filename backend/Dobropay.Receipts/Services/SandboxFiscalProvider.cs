using Dobropay.Contracts.Constants;
using Dobropay.Receipts.Models;

namespace Dobropay.Receipts.Services;

public sealed class SandboxFiscalProvider(TimeProvider? timeProvider = null) : IFiscalProvider
{
    private const string DefaultFiscalStorageNumber = "9999078900012345";
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public Task<FiscalRegistrationResult> RegisterReceiptAsync(
        FiscalRegistrationCommand command,
        CancellationToken cancellationToken)
    {
        var receiptBytes = command.ReceiptId.ToByteArray();
        var fd = Math.Abs((long)BitConverter.ToInt64(receiptBytes, 0) % 900000L) + 1000L;
        var fp = (Math.Abs(BitConverter.ToInt64(receiptBytes, 8)) % 9000000000L + 1000000000L).ToString("D10");
        var fn = DefaultFiscalStorageNumber;
        var registeredAt = _timeProvider.GetUtcNow();

        var rubles = command.TotalAmount / 100;
        var kopecks = Math.Abs(command.TotalAmount % 100);
        var operationType = command.ReceiptType == ReceiptTypes.Refund ? 2 : 1;

        var qrPayload = FormattableString.Invariant(
            $"t={registeredAt:yyyyMMddTHHmm}&s={rubles}.{kopecks:D2}&fn={fn}&i={fd}&fp={fp}&n={operationType}");

        var result = FiscalRegistrationResult.Succeeded(
            fd,
            fn,
            fp,
            fp,
            qrPayload,
            registeredAt);

        return Task.FromResult(result);
    }
}
