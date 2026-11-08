using Dobropay.Contracts.Constants;
using Microsoft.EntityFrameworkCore;

namespace Dobropay.Payments.Persistence;

public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CheckoutLink> CheckoutLinks => Set<CheckoutLink>();
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<ProviderInbox> ProviderInbox => Set<ProviderInbox>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<SavedPaymentMethod> SavedPaymentMethods => Set<SavedPaymentMethod>();
    public DbSet<PaymentReceiptPayload> PaymentReceiptPayloads => Set<PaymentReceiptPayload>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable(PaymentsSchemaConstants.PaymentsTable).HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.ShopId, x.Id });
            entity.HasIndex(x => new { x.ShopId, x.CreatedAt });
            entity.HasIndex(x => new { x.Status, x.ExpiresAt });
            entity.Property(x => x.Capture).HasDefaultValue(true);
            entity.Property(x => x.RefundedAmount).HasDefaultValue(0L);
            entity.Property(x => x.SavePaymentMethod).HasDefaultValue(false);
            entity.Property(x => x.PaymentMethodType)
                .HasMaxLength(PaymentsSchemaConstants.PaymentMethodTypeMaxLength)
                .HasDefaultValue(PaymentMethodTypes.Sandbox);
            entity.Property(x => x.ConfirmationType)
                .HasMaxLength(PaymentsSchemaConstants.ConfirmationTypeMaxLength)
                .HasDefaultValue(ConfirmationTypes.Redirect);
            entity.Property(x => x.ExpiresAt).HasDefaultValueSql(PaymentsSchemaConstants.NowDefaultValueExpression);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql(PaymentsSchemaConstants.NowDefaultValueExpression);
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql(PaymentsSchemaConstants.NowDefaultValueExpression);
            entity.Property(x => x.Currency).HasMaxLength(PaymentsSchemaConstants.CurrencyMaxLength);
            entity.Property(x => x.Status).HasMaxLength(PaymentsSchemaConstants.StatusMaxLength);
            entity.Property(x => x.ExternalOrderId).HasMaxLength(PaymentsSchemaConstants.ExternalOrderIdMaxLength);
            entity.Property(x => x.CancellationParty).HasMaxLength(PaymentsSchemaConstants.CancellationPartyMaxLength);
            entity.Property(x => x.CancellationReason).HasMaxLength(PaymentsSchemaConstants.CancellationReasonMaxLength);
        });

        modelBuilder.Entity<CheckoutLink>(entity =>
        {
            entity.ToTable(PaymentsSchemaConstants.CheckoutLinksTable).HasKey(x => x.TokenHash);
            entity.HasOne<Payment>()
                .WithMany()
                .HasForeignKey(x => new { x.ShopId, x.PaymentId })
                .HasPrincipalKey(x => new { x.ShopId, x.Id });
            entity.HasIndex(x => new { x.ShopId, x.PaymentId }).IsUnique();
        });

        modelBuilder.Entity<PaymentAttempt>(entity =>
        {
            entity.ToTable(PaymentsSchemaConstants.PaymentAttemptsTable).HasKey(x => x.Id);
            entity.HasOne<Payment>()
                .WithMany()
                .HasForeignKey(x => new { x.ShopId, x.PaymentId })
                .HasPrincipalKey(x => new { x.ShopId, x.Id });
            entity.HasIndex(x => new { x.ShopId, x.PaymentId }).IsUnique();
            entity.HasIndex(x => x.ExternalId).IsUnique();
            entity.HasIndex(x => x.OperationKey).IsUnique();
            entity.Property(x => x.PaymentMethodType)
                .HasMaxLength(PaymentsSchemaConstants.PaymentMethodTypeMaxLength)
                .HasDefaultValue(PaymentMethodTypes.Sandbox);
            entity.Property(x => x.SbpQrcId).HasMaxLength(PaymentsSchemaConstants.SbpIdMaxLength);
            entity.Property(x => x.SbpOperationId).HasMaxLength(PaymentsSchemaConstants.SbpIdMaxLength);
            entity.Property(x => x.PayerBankId).HasMaxLength(PaymentsSchemaConstants.SbpIdMaxLength);
            entity.Property(x => x.CryptoCurrency).HasMaxLength(PaymentsSchemaConstants.CryptoCurrencyMaxLength);
            entity.Property(x => x.CryptoNetwork).HasMaxLength(PaymentsSchemaConstants.CryptoNetworkMaxLength);
            entity.Property(x => x.CryptoAmount).HasMaxLength(PaymentsSchemaConstants.CryptoAmountMaxLength);
            entity.Property(x => x.DepositAddress).HasMaxLength(PaymentsSchemaConstants.DepositAddressMaxLength);
            entity.Property(x => x.TxHash).HasMaxLength(PaymentsSchemaConstants.TxHashMaxLength);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql(PaymentsSchemaConstants.NowDefaultValueExpression);
        });

        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.ToTable(PaymentsSchemaConstants.IdempotencyRecordsTable)
                .HasKey(x => new { x.ShopId, x.Operation, x.IdempotencyKey });
            entity.HasOne<Payment>()
                .WithMany()
                .HasForeignKey(x => new { x.ShopId, x.PaymentId })
                .HasPrincipalKey(x => new { x.ShopId, x.Id });
        });

        modelBuilder.Entity<ProviderInbox>(entity =>
        {
            entity.ToTable(PaymentsSchemaConstants.ProviderInboxTable).HasKey(x => x.Id);
            entity.HasIndex(x => x.ProviderEventId).IsUnique();
            entity.HasIndex(x => new { x.State, x.NextAt });
            entity.Property(x => x.Payload).HasColumnType(PaymentsSchemaConstants.JsonbColumnType);
        });

        modelBuilder.Entity<OutboxEvent>(entity =>
        {
            entity.ToTable(PaymentsSchemaConstants.OutboxEventsTable).HasKey(x => x.Id);
            entity.HasIndex(x => new { x.Published, x.OccurredAt });
            entity.Property(x => x.Envelope).HasColumnType(PaymentsSchemaConstants.JsonbColumnType);
        });

        modelBuilder.ApplyConfiguration(new RefundConfiguration());
        modelBuilder.ApplyConfiguration(new SavedPaymentMethodConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentReceiptPayloadConfiguration());

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
