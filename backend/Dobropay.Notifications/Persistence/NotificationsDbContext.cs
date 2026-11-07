using Microsoft.EntityFrameworkCore;

namespace Dobropay.Notifications.Persistence;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options)
{
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<NotificationInbox> NotificationInbox => Set<NotificationInbox>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
    public DbSet<WebhookDeliveryAttempt> WebhookDeliveryAttempts => Set<WebhookDeliveryAttempt>();
    public DbSet<LocalReceiverEvent> LocalReceiverEvents => Set<LocalReceiverEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WebhookSubscription>()
            .ToTable(NotificationsSchemaConstants.WebhookSubscriptionsTable)
            .HasKey(x => x.Id);

        modelBuilder.Entity<NotificationInbox>(entity =>
        {
            entity.ToTable(NotificationsSchemaConstants.NotificationInboxTable).HasKey(x => x.EventId);
            entity.Property(x => x.Envelope).HasColumnType(NotificationsSchemaConstants.JsonbColumnType);
        });

        modelBuilder.Entity<WebhookDelivery>(entity =>
        {
            entity.ToTable(NotificationsSchemaConstants.WebhookDeliveriesTable).HasKey(x => x.Id);
            entity.HasIndex(x => new { x.EventId, x.SubscriptionId }).IsUnique();
            entity.HasIndex(x => new { x.State, x.NextAt, x.LockedUntil });
        });

        modelBuilder.Entity<WebhookDeliveryAttempt>()
            .ToTable(NotificationsSchemaConstants.WebhookDeliveryAttemptsTable)
            .HasKey(x => x.Id);

        modelBuilder.Entity<LocalReceiverEvent>(entity =>
        {
            entity.ToTable(NotificationsSchemaConstants.LocalReceiverEventsTable).HasKey(x => x.EventId);
            entity.Property(x => x.Body).HasColumnType(NotificationsSchemaConstants.JsonbColumnType);
        });

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
