using Dobropay.Receipts.Models;
using Dobropay.Receipts.Persistence;

namespace Dobropay.Receipts.Persistence;

public interface IReceiptsRepository
{
    Task<bool> HasInboxEventAsync(Guid eventId, CancellationToken cancellationToken);
    Task RecordInboxEventAsync(Guid eventId, Guid shopId, Guid paymentId, CancellationToken cancellationToken);
    Task SaveFiscalReceiptAsync(FiscalReceipt receipt, CancellationToken cancellationToken);
    Task<FiscalReceipt?> GetFiscalReceiptByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<FiscalReceipt?> LookupFiscalReceiptAsync(Guid? id, Guid? paymentId, long? fiscalDocumentNumber, CancellationToken cancellationToken);
    Task<IReadOnlyList<FiscalReceipt>> ListFiscalReceiptsByShopAsync(Guid shopId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DemoReceiptView>> ListReceiptsByShopAsync(Guid shopId, CancellationToken cancellationToken);
    Task SaveIncomingPaymentEventAsync(Guid eventId, Guid shopId, Guid paymentId, bool isSucceeded, long amount, string currency, CancellationToken cancellationToken);
    Task<DemoReceipt?> AcquireNextPendingReceiptAsync(CancellationToken cancellationToken);
    Task CompleteReceiptEmailAsync(Guid receiptId, string finalState, CancellationToken cancellationToken);
}
