
namespace Dobropay.Receipts.Persistence;

public interface IReceiptsRepository
{
    Task<bool> HasInboxEventAsync(Guid eventId, CancellationToken cancellationToken);
    Task RecordInboxEventAsync(Guid eventId, Guid shopId, Guid paymentId, CancellationToken cancellationToken);
    Task SaveFiscalReceiptAsync(FiscalReceipt receipt, CancellationToken cancellationToken);
    Task<FiscalReceipt?> GetFiscalReceiptByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<FiscalReceipt?> LookupFiscalReceiptAsync(Guid? id, Guid? paymentId, long? fiscalDocumentNumber, CancellationToken cancellationToken);
    Task<IReadOnlyList<FiscalReceipt>> ListFiscalReceiptsByShopAsync(Guid shopId, CancellationToken cancellationToken);

}
