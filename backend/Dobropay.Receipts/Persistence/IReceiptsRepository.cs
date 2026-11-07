using Dobropay.Receipts.Models;

namespace Dobropay.Receipts.Persistence;

public interface IReceiptsRepository
{
    Task<IReadOnlyList<DemoReceiptView>> ListReceiptsByShopAsync(Guid shopId, CancellationToken cancellationToken);
    Task SaveIncomingPaymentEventAsync(Guid eventId, Guid shopId, Guid paymentId, bool isSucceeded, long amount, string currency, CancellationToken cancellationToken);
    Task<DemoReceipt?> AcquireNextPendingReceiptAsync(CancellationToken cancellationToken);
    Task CompleteReceiptEmailAsync(Guid receiptId, string finalState, CancellationToken cancellationToken);
}
