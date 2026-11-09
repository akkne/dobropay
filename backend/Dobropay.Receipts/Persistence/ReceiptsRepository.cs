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

}
