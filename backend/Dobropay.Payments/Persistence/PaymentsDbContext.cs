using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dobropay.Payments.Persistence;

public sealed class Payment
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public long Amount { get; set; }
    public string Currency { get; set; } = "RUB";
    public string? ExternalOrderId { get; set; }
    public string Status { get; set; } = "pending";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class CheckoutLink
{
    public string TokenHash { get; set; } = "";
    public Guid ShopId { get; set; }
    public Guid PaymentId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed class PaymentAttempt
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid OperationKey { get; set; }
    public string? ExternalId { get; set; }
    public string State { get; set; } = "unknown";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class IdempotencyRecord
{
    public Guid ShopId { get; set; }
    public string Operation { get; set; } = "";
    public string IdempotencyKey { get; set; } = "";
    public string RequestHash { get; set; } = "";
    public Guid PaymentId { get; set; }
}

public sealed class ProviderInbox
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid PaymentId { get; set; }
    public string ProviderEventId { get; set; } = "";
    public string Payload { get; set; } = "{}";
    public string State { get; set; } = "accepted";
    public int Attempts { get; set; }
    public DateTimeOffset NextAt { get; set; }
    public string? Error { get; set; }
}

public sealed class OutboxEvent
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid PaymentId { get; set; }
    public string Topic { get; set; } = "";
    public string Envelope { get; set; } = "{}";
    public DateTimeOffset OccurredAt { get; set; }
    public bool Published { get; set; }
}

public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CheckoutLink> CheckoutLinks => Set<CheckoutLink>();
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<ProviderInbox> ProviderInbox => Set<ProviderInbox>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Payment>().ToTable("payments").HasKey(x => x.Id);
        b.Entity<Payment>().HasAlternateKey(x => new { x.ShopId, x.Id });
        b.Entity<Payment>().HasIndex(x => new { x.ShopId, x.CreatedAt });
        b.Entity<Payment>().Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        b.Entity<Payment>().Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
        b.Entity<Payment>().Property(x => x.Currency).HasMaxLength(3);
        b.Entity<Payment>().Property(x => x.Status).HasMaxLength(16);
        b.Entity<Payment>().Property(x => x.ExternalOrderId).HasMaxLength(200);

        b.Entity<CheckoutLink>().ToTable("checkout_links").HasKey(x => x.TokenHash);
        b.Entity<CheckoutLink>().HasOne<Payment>().WithMany().HasForeignKey(x => new { x.ShopId, x.PaymentId }).HasPrincipalKey(x => new { x.ShopId, x.Id });
        b.Entity<CheckoutLink>().HasIndex(x => new { x.ShopId, x.PaymentId }).IsUnique();

        b.Entity<PaymentAttempt>().ToTable("payment_attempts").HasKey(x => x.Id);
        b.Entity<PaymentAttempt>().HasOne<Payment>().WithMany().HasForeignKey(x => new { x.ShopId, x.PaymentId }).HasPrincipalKey(x => new { x.ShopId, x.Id });
        b.Entity<PaymentAttempt>().HasIndex(x => new { x.ShopId, x.PaymentId }).IsUnique();
        b.Entity<PaymentAttempt>().HasIndex(x => x.ExternalId).IsUnique();
        b.Entity<PaymentAttempt>().HasIndex(x => x.OperationKey).IsUnique();
        b.Entity<PaymentAttempt>().Property(x => x.CreatedAt).HasDefaultValueSql("now()");

        b.Entity<IdempotencyRecord>().ToTable("idempotency_records").HasKey(x => new { x.ShopId, x.Operation, x.IdempotencyKey });
        b.Entity<IdempotencyRecord>().HasOne<Payment>().WithMany().HasForeignKey(x => new { x.ShopId, x.PaymentId }).HasPrincipalKey(x => new { x.ShopId, x.Id });

        b.Entity<ProviderInbox>().ToTable("provider_inbox").HasKey(x => x.Id);
        b.Entity<ProviderInbox>().HasIndex(x => x.ProviderEventId).IsUnique();
        b.Entity<ProviderInbox>().HasIndex(x => new { x.State, x.NextAt });
        b.Entity<ProviderInbox>().Property(x => x.Payload).HasColumnType("jsonb");

        b.Entity<OutboxEvent>().ToTable("outbox_events").HasKey(x => x.Id);
        b.Entity<OutboxEvent>().HasIndex(x => new { x.Published, x.OccurredAt });
        b.Entity<OutboxEvent>().Property(x => x.Envelope).HasColumnType("jsonb");

        foreach (var e in b.Model.GetEntityTypes())
            foreach (var p in e.GetProperties()) p.SetColumnName(ToSnake(p.Name));
    }

    private static string ToSnake(string name) => System.Text.RegularExpressions.Regex.Replace(name, "(?<!^)([A-Z])", "_$1").ToLowerInvariant();
}

public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<PaymentsDbContext>
{
    public PaymentsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<PaymentsDbContext>()
        .UseNpgsql("Host=localhost;Database=payments_db;Username=postgres;Password=local-postgres-password").Options);
}
