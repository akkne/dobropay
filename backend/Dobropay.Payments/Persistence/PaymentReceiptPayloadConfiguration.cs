using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dobropay.Payments.Persistence;

public sealed class PaymentReceiptPayloadConfiguration : IEntityTypeConfiguration<PaymentReceiptPayload>
{
    public void Configure(EntityTypeBuilder<PaymentReceiptPayload> builder)
    {
        builder.ToTable(PaymentsSchemaConstants.PaymentReceiptPayloadsTable).HasKey(x => x.Id);
        builder.HasOne<Payment>()
            .WithMany()
            .HasForeignKey(x => new { x.ShopId, x.PaymentId })
            .HasPrincipalKey(x => new { x.ShopId, x.Id })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.ShopId, x.PaymentId, x.RefundId });
        builder.Property(x => x.ReceiptType).HasMaxLength(PaymentsSchemaConstants.ReceiptTypeMaxLength);
        builder.Property(x => x.CustomerEmail).HasMaxLength(PaymentsSchemaConstants.CustomerEmailMaxLength);
        builder.Property(x => x.CustomerPhone).HasMaxLength(PaymentsSchemaConstants.CustomerPhoneMaxLength);
        builder.Property(x => x.ItemsJson).HasColumnType(PaymentsSchemaConstants.JsonbColumnType);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql(PaymentsSchemaConstants.NowDefaultValueExpression);
    }
}
