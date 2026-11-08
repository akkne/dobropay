using Microsoft.EntityFrameworkCore;

namespace Dobropay.Receipts.Persistence;

public sealed class ReceiptsDbContext(DbContextOptions<ReceiptsDbContext> options) : DbContext(options)
{
    public DbSet<ReceiptInbox> ReceiptInbox => Set<ReceiptInbox>();
    public DbSet<DemoReceipt> DemoReceipts => Set<DemoReceipt>();
    public DbSet<FiscalReceipt> FiscalReceipts => Set<FiscalReceipt>();
    public DbSet<FiscalReceiptItem> FiscalReceiptItems => Set<FiscalReceiptItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReceiptInbox>()
            .ToTable(ReceiptsSchemaConstants.ReceiptInboxTable)
            .HasKey(x => x.EventId);

        modelBuilder.Entity<DemoReceipt>(entity =>
        {
            entity.ToTable(ReceiptsSchemaConstants.DemoReceiptsTable).HasKey(x => x.Id);
            entity.HasIndex(x => x.EventId).IsUnique();
            entity.HasIndex(x => new { x.ShopId, x.PaymentId });
        });

        modelBuilder.ApplyConfiguration(new FiscalReceiptConfiguration());
        modelBuilder.ApplyConfiguration(new FiscalReceiptItemConfiguration());

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }
        }
    }

    private static string ToSnakeCase(string name) =>
        System.Text.RegularExpressions.Regex.Replace(name, "(?<!^)([A-Z])", "_$1").ToLowerInvariant();
}
