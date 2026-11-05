using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dobropay.Notifications.Persistence;

public sealed class WebhookSubscription
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string Url { get; set; } = "";
    public string SigningSecret { get; set; } = "";
}

public sealed class NotificationInbox
{
    public Guid EventId { get; set; }
    public Guid ShopId { get; set; }
    public Guid PaymentId { get; set; }
    public string Envelope { get; set; } = "{}";
    public DateTimeOffset ReceivedAt { get; set; }
}

public sealed class WebhookDelivery
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid EventId { get; set; }
    public Guid SubscriptionId { get; set; }
    public string State { get; set; } = "pending";
    public int Attempts { get; set; }
    public DateTimeOffset NextAt { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public string? LastResult { get; set; }
}

public sealed class WebhookDeliveryAttempt
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid DeliveryId { get; set; }
    public DateTimeOffset AttemptedAt { get; set; }
    public int? HttpStatus { get; set; }
    public string Result { get; set; } = "";
}

public sealed class LocalReceiverEvent
{
    public Guid EventId { get; set; }
    public int Attempts { get; set; }
    public bool Verified { get; set; }
    public string? Body { get; set; }
}

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options)
{
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<NotificationInbox> NotificationInbox => Set<NotificationInbox>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
    public DbSet<WebhookDeliveryAttempt> WebhookDeliveryAttempts => Set<WebhookDeliveryAttempt>();
    public DbSet<LocalReceiverEvent> LocalReceiverEvents => Set<LocalReceiverEvent>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<WebhookSubscription>().ToTable("webhook_subscriptions").HasKey(x => x.Id);
        b.Entity<NotificationInbox>().ToTable("notification_inbox").HasKey(x => x.EventId);
        b.Entity<NotificationInbox>().Property(x => x.Envelope).HasColumnType("jsonb");
        b.Entity<WebhookDelivery>().ToTable("webhook_deliveries").HasKey(x => x.Id);
        b.Entity<WebhookDelivery>().HasIndex(x => new { x.EventId, x.SubscriptionId }).IsUnique();
        b.Entity<WebhookDelivery>().HasIndex(x => new { x.State, x.NextAt, x.LockedUntil });
        b.Entity<WebhookDeliveryAttempt>().ToTable("webhook_delivery_attempts").HasKey(x => x.Id);
        b.Entity<LocalReceiverEvent>().ToTable("local_receiver_events").HasKey(x => x.EventId);
        b.Entity<LocalReceiverEvent>().Property(x => x.Body).HasColumnType("jsonb");
        foreach (var e in b.Model.GetEntityTypes())
            foreach (var p in e.GetProperties()) p.SetColumnName(System.Text.RegularExpressions.Regex.Replace(p.Name, "(?<!^)([A-Z])", "_$1").ToLowerInvariant());
    }
}

public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<NotificationsDbContext>
{
    public NotificationsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<NotificationsDbContext>()
        .UseNpgsql("Host=localhost;Database=notifications_db;Username=postgres;Password=local-postgres-password").Options);
}
