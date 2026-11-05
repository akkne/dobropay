using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dobropay.Receipts.Persistence;

public sealed class ReceiptInbox
{
    public Guid EventId { get; set; }
    public Guid ShopId { get; set; }
    public Guid PaymentId { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
}

public sealed class DemoReceipt
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid EventId { get; set; }
    public long Amount { get; set; }
    public string Currency { get; set; } = "RUB";
    public string EmailState { get; set; } = "pending";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ReceiptsDbContext(DbContextOptions<ReceiptsDbContext> options) : DbContext(options)
{
    public DbSet<ReceiptInbox> ReceiptInbox => Set<ReceiptInbox>();
    public DbSet<DemoReceipt> DemoReceipts => Set<DemoReceipt>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<ReceiptInbox>().ToTable("receipt_inbox").HasKey(x => x.EventId);
        b.Entity<DemoReceipt>().ToTable("demo_receipts").HasKey(x => x.Id);
        b.Entity<DemoReceipt>().HasIndex(x => x.EventId).IsUnique();
        b.Entity<DemoReceipt>().HasIndex(x => new { x.ShopId, x.PaymentId });
        foreach (var e in b.Model.GetEntityTypes())
            foreach (var p in e.GetProperties()) p.SetColumnName(System.Text.RegularExpressions.Regex.Replace(p.Name, "(?<!^)([A-Z])", "_$1").ToLowerInvariant());
    }
}

public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<ReceiptsDbContext>
{
    public ReceiptsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<ReceiptsDbContext>()
        .UseNpgsql("Host=localhost;Database=receipts_db;Username=postgres;Password=local-postgres-password").Options);
}
