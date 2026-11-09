using Dobropay.Receipts.Models;
using Dobropay.Receipts.Persistence;

namespace Dobropay.UnitTests.Receipts;

public sealed class FakeReceiptsRepository : IReceiptsRepository
{
    public List<FiscalReceipt> FiscalReceipts { get; } = new();
    public List<ReceiptInbox> Inboxes { get; } = new();

    public Task<bool> HasInboxEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        Task.FromResult(Inboxes.Any(x => x.EventId == eventId));

    public Task RecordInboxEventAsync(Guid eventId, Guid shopId, Guid paymentId, CancellationToken cancellationToken)
    {
        if (!Inboxes.Any(x => x.EventId == eventId))
        {
            Inboxes.Add(new ReceiptInbox
            {
                EventId = eventId,
                ShopId = shopId,
                PaymentId = paymentId,
                ReceivedAt = DateTimeOffset.UtcNow
            });
        }

        return Task.CompletedTask;
    }

    public Task SaveFiscalReceiptAsync(FiscalReceipt receipt, CancellationToken cancellationToken)
    {
        FiscalReceipts.Add(receipt);
        return Task.CompletedTask;
    }

    public Task<FiscalReceipt?> GetFiscalReceiptByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(FiscalReceipts.FirstOrDefault(x => x.Id == id));

    public Task<FiscalReceipt?> LookupFiscalReceiptAsync(
        Guid? id,
        Guid? paymentId,
        long? fiscalDocumentNumber,
        CancellationToken cancellationToken)
    {
        if (id.HasValue && id.Value != Guid.Empty)
        {
            return Task.FromResult(FiscalReceipts.FirstOrDefault(x => x.Id == id.Value));
        }

        if (paymentId.HasValue && paymentId.Value != Guid.Empty)
        {
            return Task.FromResult(FiscalReceipts
                .Where(x => x.PaymentId == paymentId.Value)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault());
        }

        if (fiscalDocumentNumber.HasValue)
        {
            return Task.FromResult(FiscalReceipts
                .Where(x => x.FiscalDocumentNumber == fiscalDocumentNumber.Value)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault());
        }

        return Task.FromResult<FiscalReceipt?>(null);
    }

    public Task<IReadOnlyList<FiscalReceipt>> ListFiscalReceiptsByShopAsync(Guid shopId, CancellationToken cancellationToken)
    {
        var list = FiscalReceipts
            .Where(x => x.ShopId == shopId)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<FiscalReceipt>>(list);
    }

}
