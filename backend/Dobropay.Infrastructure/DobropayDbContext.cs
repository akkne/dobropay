using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dobropay.Infrastructure;

public sealed class Shop { public Guid Id { get; set; } public string Name { get; set; } = ""; }
public sealed class ApiCredential { public Guid Id { get; set; } public Guid ShopId { get; set; } public string KeyHash { get; set; } = ""; }
public sealed class Payment { public Guid Id { get; set; } public Guid ShopId { get; set; } public long Amount { get; set; } public string Currency { get; set; } = "RUB"; public string? ExternalOrderId { get; set; } public string Status { get; set; } = "pending"; public DateTimeOffset CreatedAt { get; set; } public DateTimeOffset UpdatedAt { get; set; } }
public sealed class CheckoutLink { public string TokenHash { get; set; } = ""; public Guid ShopId { get; set; } public Guid PaymentId { get; set; } public DateTimeOffset ExpiresAt { get; set; } }
public sealed class IdempotencyRecord { public Guid ShopId { get; set; } public string Operation { get; set; } = ""; public string IdempotencyKey { get; set; } = ""; public string RequestHash { get; set; } = ""; public Guid PaymentId { get; set; } }
public sealed class PaymentAttempt { public Guid Id { get; set; } public Guid ShopId { get; set; } public Guid PaymentId { get; set; } public Guid OperationKey { get; set; } public string? ExternalId { get; set; } public string State { get; set; } = "unknown"; public DateTimeOffset CreatedAt { get; set; } }
public sealed class ProviderInbox { public Guid Id { get; set; } public string ProviderEventId { get; set; } = ""; public Guid ShopId { get; set; } public string Payload { get; set; } = "{}"; public DateTimeOffset ReceivedAt { get; set; } public string State { get; set; } = "pending"; public int Attempts { get; set; } public DateTimeOffset NextAt { get; set; } public string? Error { get; set; } }
public sealed class OutboxEvent { public Guid Id { get; set; } public Guid ShopId { get; set; } public Guid PaymentId { get; set; } public string EventType { get; set; } = ""; public DateTimeOffset OccurredAt { get; set; } public bool Queued { get; set; } }
public sealed class WebhookSubscription { public Guid Id { get; set; } public Guid ShopId { get; set; } public string Url { get; set; } = ""; public string SigningSecret { get; set; } = ""; }
public sealed class WebhookDelivery { public Guid Id { get; set; } public Guid ShopId { get; set; } public Guid EventId { get; set; } public Guid SubscriptionId { get; set; } public string State { get; set; } = "pending"; public int Attempts { get; set; } public DateTimeOffset NextAt { get; set; } public DateTimeOffset? LockedUntil { get; set; } public string? LastResult { get; set; } }
public sealed class WebhookDeliveryAttempt { public Guid Id { get; set; } public Guid ShopId { get; set; } public Guid DeliveryId { get; set; } public DateTimeOffset AttemptedAt { get; set; } public int? HttpStatus { get; set; } public string Result { get; set; } = ""; }
public sealed class LocalReceiverEvent { public Guid EventId { get; set; } public int Attempts { get; set; } public bool Verified { get; set; } public string? Body { get; set; } }

public sealed class DobropayDbContext(DbContextOptions<DobropayDbContext> options) : DbContext(options)
{
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<ApiCredential> ApiCredentials => Set<ApiCredential>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CheckoutLink> CheckoutLinks => Set<CheckoutLink>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();
    public DbSet<ProviderInbox> ProviderInbox => Set<ProviderInbox>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
    public DbSet<WebhookDeliveryAttempt> WebhookDeliveryAttempts => Set<WebhookDeliveryAttempt>();
    public DbSet<LocalReceiverEvent> LocalReceiverEvents => Set<LocalReceiverEvent>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Shop>().HasKey(x => x.Id);
        b.Entity<ApiCredential>().HasKey(x => x.Id);
        b.Entity<ApiCredential>().HasIndex(x => x.KeyHash).IsUnique();
        b.Entity<Payment>().HasKey(x => x.Id);
        b.Entity<Payment>().HasAlternateKey(x => new { x.ShopId, x.Id });
        b.Entity<Payment>().HasIndex(x => new { x.ShopId, x.CreatedAt });
        b.Entity<Payment>().Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        b.Entity<Payment>().Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
        b.Entity<CheckoutLink>().HasKey(x => x.TokenHash);
        b.Entity<CheckoutLink>().HasIndex(x => new { x.ShopId, x.PaymentId }).IsUnique();
        b.Entity<CheckoutLink>().HasOne<Payment>().WithMany().HasForeignKey(x => new { x.ShopId, x.PaymentId }).HasPrincipalKey(x => new { x.ShopId, x.Id });
        b.Entity<IdempotencyRecord>().HasKey(x => new { x.ShopId, x.Operation, x.IdempotencyKey });
        b.Entity<PaymentAttempt>().HasKey(x => x.Id);
        b.Entity<PaymentAttempt>().HasAlternateKey(x => new { x.ShopId, x.Id });
        b.Entity<PaymentAttempt>().HasIndex(x => x.OperationKey).IsUnique();
        b.Entity<PaymentAttempt>().HasIndex(x => x.ExternalId).IsUnique();
        b.Entity<PaymentAttempt>().HasIndex(x => new { x.ShopId, x.PaymentId });
        b.Entity<PaymentAttempt>().HasOne<Payment>().WithMany().HasForeignKey(x => new { x.ShopId, x.PaymentId }).HasPrincipalKey(x => new { x.ShopId, x.Id });
        b.Entity<PaymentAttempt>().Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        b.Entity<ProviderInbox>().HasKey(x => x.Id);
        b.Entity<ProviderInbox>().HasIndex(x => x.ProviderEventId).IsUnique();
        b.Entity<ProviderInbox>().HasIndex(x => new { x.State, x.NextAt });
        b.Entity<ProviderInbox>().Property(x => x.Payload).HasColumnType("jsonb");
        b.Entity<ProviderInbox>().Property(x => x.ReceivedAt).HasDefaultValueSql("now()");
        b.Entity<ProviderInbox>().Property(x => x.NextAt).HasDefaultValueSql("now()");
        b.Entity<OutboxEvent>().HasKey(x => x.Id);
        b.Entity<OutboxEvent>().HasIndex(x => new { x.Queued, x.OccurredAt });
        b.Entity<OutboxEvent>().Property(x => x.OccurredAt).HasDefaultValueSql("now()");
        b.Entity<WebhookSubscription>().HasKey(x => x.Id);
        b.Entity<WebhookDelivery>().HasKey(x => x.Id);
        b.Entity<WebhookDelivery>().HasIndex(x => new { x.EventId, x.SubscriptionId }).IsUnique();
        b.Entity<WebhookDelivery>().HasIndex(x => new { x.State, x.NextAt, x.LockedUntil });
        b.Entity<WebhookDelivery>().Property(x => x.NextAt).HasDefaultValueSql("now()");
        b.Entity<WebhookDeliveryAttempt>().HasKey(x => x.Id);
        b.Entity<WebhookDeliveryAttempt>().Property(x => x.AttemptedAt).HasDefaultValueSql("now()");
        b.Entity<LocalReceiverEvent>().HasKey(x => x.EventId);
        b.Entity<LocalReceiverEvent>().Property(x => x.Body).HasColumnType("jsonb");
        foreach (var entity in b.Model.GetEntityTypes())
        {
            var name = Snake(entity.ClrType.Name);
            entity.SetTableName(name switch { "provider_inbox" => "provider_inbox", "webhook_delivery" => "webhook_deliveries", "webhook_delivery_attempt" => "webhook_delivery_attempts", _ => name + "s" });
            foreach (var property in entity.GetProperties()) property.SetColumnName(Snake(property.Name));
        }
    }

    private static string Snake(string value) => Regex.Replace(value, "(?<!^)([A-Z])", "_$1").ToLowerInvariant();
}

public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<DobropayDbContext>
{
    public DobropayDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<DobropayDbContext>()
        .UseNpgsql("Host=localhost;Database=dobropay;Username=postgres;Password=local-postgres-password").Options);
}
