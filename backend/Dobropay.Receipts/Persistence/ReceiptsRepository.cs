using Dobropay.Receipts.Models;
using Microsoft.EntityFrameworkCore;

namespace Dobropay.Receipts.Persistence;

public sealed class ReceiptsRepository(ReceiptsDbContext dbContext) : IReceiptsRepository
{
    public async Task<bool> HasInboxEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        await dbContext.ReceiptInbox
            .AsNoTracking()
            .AnyAsync(x => x.EventId == eventId, cancellationToken);

    public async Task RecordInboxEventAsync(Guid eventId, Guid shopId, Guid paymentId, CancellationToken cancellationToken)
    {
        var exists = await HasInboxEventAsync(eventId, cancellationToken);
        if (exists)
        {
            return;
        }

        await dbContext.ReceiptInbox.AddAsync(
            new ReceiptInbox
            {
                EventId = eventId,
                ShopId = shopId,
                PaymentId = paymentId,
                ReceivedAt = DateTimeOffset.UtcNow
            },
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveFiscalReceiptAsync(FiscalReceipt receipt, CancellationToken cancellationToken)
    {
        await dbContext.FiscalReceipts.AddAsync(receipt, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<FiscalReceipt?> GetFiscalReceiptByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.FiscalReceipts
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<FiscalReceipt?> LookupFiscalReceiptAsync(
        Guid? id,
        Guid? paymentId,
        long? fiscalDocumentNumber,
        CancellationToken cancellationToken)
    {
        var query = dbContext.FiscalReceipts
            .AsNoTracking()
            .Include(x => x.Items)
            .AsQueryable();

        if (id.HasValue && id.Value != Guid.Empty)
        {
            return await query.FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken);
        }

        if (paymentId.HasValue && paymentId.Value != Guid.Empty)
        {
            return await query
                .Where(x => x.PaymentId == paymentId.Value)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (fiscalDocumentNumber.HasValue)
        {
            return await query
                .Where(x => x.FiscalDocumentNumber == fiscalDocumentNumber.Value)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return null;
    }

    public async Task<IReadOnlyList<FiscalReceipt>> ListFiscalReceiptsByShopAsync(Guid shopId, CancellationToken cancellationToken) =>
        await dbContext.FiscalReceipts
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.ShopId == shopId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DemoReceiptView>> ListReceiptsByShopAsync(
        Guid shopId,
        CancellationToken cancellationToken) =>
        await dbContext.DemoReceipts
            .AsNoTracking()
            .Where(x => x.ShopId == shopId)
            .Select(x => new DemoReceiptView(x.Id, x.PaymentId, x.Amount, x.Currency, x.EmailState))
            .ToListAsync(cancellationToken);

    public async Task SaveIncomingPaymentEventAsync(
        Guid eventId,
        Guid shopId,
        Guid paymentId,
        bool isSucceeded,
        long amount,
        string currency,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.ReceiptInbox
            .AsNoTracking()
            .AnyAsync(x => x.EventId == eventId, cancellationToken);
        if (exists)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        await dbContext.ReceiptInbox.AddAsync(
            new ReceiptInbox
            {
                EventId = eventId,
                ShopId = shopId,
                PaymentId = paymentId,
                ReceivedAt = now
            },
            cancellationToken);

        if (isSucceeded)
        {
            await dbContext.DemoReceipts.AddAsync(
                new DemoReceipt
                {
                    Id = Guid.NewGuid(),
                    ShopId = shopId,
                    PaymentId = paymentId,
                    EventId = eventId,
                    Amount = amount,
                    Currency = currency,
                    EmailState = ReceiptsSchemaConstants.EmailStatePending,
                    CreatedAt = now
                },
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<DemoReceipt?> AcquireNextPendingReceiptAsync(CancellationToken cancellationToken)
    {
        var receipt = await dbContext.DemoReceipts
            .Where(x => x.EmailState == ReceiptsSchemaConstants.EmailStatePending)
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (receipt is null)
        {
            return null;
        }

        receipt.EmailState = ReceiptsSchemaConstants.EmailStateSending;
        await dbContext.SaveChangesAsync(cancellationToken);
        return receipt;
    }

    public async Task CompleteReceiptEmailAsync(
        Guid receiptId,
        string finalState,
        CancellationToken cancellationToken)
    {
        var receipt = await dbContext.DemoReceipts
            .SingleOrDefaultAsync(
                x => x.Id == receiptId && x.EmailState == ReceiptsSchemaConstants.EmailStateSending,
                cancellationToken);
        if (receipt is null)
        {
            return;
        }

        receipt.EmailState = finalState;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
