using Dobropay.Receipts.Models;
using Dobropay.Receipts.Persistence;

namespace Dobropay.UnitTests.Receipts;

public sealed class FakeReceiptsRepository : IReceiptsRepository
{
    public List<FiscalReceipt> FiscalReceipts { get; } = new();
    public List<ReceiptInbox> Inboxes { get; } = new();
    public List<DemoReceipt> DemoReceipts { get; } = new();

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

    public Task<IReadOnlyList<DemoReceiptView>> ListReceiptsByShopAsync(Guid shopId, CancellationToken cancellationToken)
    {
        var list = DemoReceipts
            .Where(x => x.ShopId == shopId)
            .Select(x => new DemoReceiptView(x.Id, x.PaymentId, x.Amount, x.Currency, x.EmailState))
            .ToList();
        return Task.FromResult<IReadOnlyList<DemoReceiptView>>(list);
    }

    public Task SaveIncomingPaymentEventAsync(
        Guid eventId,
        Guid shopId,
        Guid paymentId,
        bool isSucceeded,
        long amount,
        string currency,
        CancellationToken cancellationToken)
    {
        Inboxes.Add(new ReceiptInbox
        {
            EventId = eventId,
            ShopId = shopId,
            PaymentId = paymentId,
            ReceivedAt = DateTimeOffset.UtcNow
        });

        if (isSucceeded)
        {
            DemoReceipts.Add(new DemoReceipt
            {
                Id = Guid.NewGuid(),
                ShopId = shopId,
                PaymentId = paymentId,
                EventId = eventId,
                Amount = amount,
                Currency = currency,
                EmailState = ReceiptsSchemaConstants.EmailStatePending,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        return Task.CompletedTask;
    }

    public Task<DemoReceipt?> AcquireNextPendingReceiptAsync(CancellationToken cancellationToken)
    {
        var receipt = DemoReceipts.FirstOrDefault(x => x.EmailState == ReceiptsSchemaConstants.EmailStatePending);
        if (receipt is not null)
        {
            receipt.EmailState = ReceiptsSchemaConstants.EmailStateSending;
        }

        return Task.FromResult(receipt);
    }

    public Task CompleteReceiptEmailAsync(Guid receiptId, string finalState, CancellationToken cancellationToken)
    {
        var receipt = DemoReceipts.FirstOrDefault(x => x.Id == receiptId);
        if (receipt is not null)
        {
            receipt.EmailState = finalState;
        }

        return Task.CompletedTask;
    }
}
