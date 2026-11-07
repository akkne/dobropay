using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dobropay.Payments.Persistence;

public sealed class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable(PaymentsSchemaConstants.RefundsTable).HasKey(x => x.Id);
        builder.HasOne<Payment>()
            .WithMany()
            .HasForeignKey(x => new { x.ShopId, x.PaymentId })
            .HasPrincipalKey(x => new { x.ShopId, x.Id })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.ShopId, x.PaymentId });
        builder.HasIndex(x => new { x.ShopId, x.CreatedAt });
        builder.Property(x => x.Currency).HasMaxLength(PaymentsSchemaConstants.CurrencyMaxLength);
        builder.Property(x => x.Status).HasMaxLength(PaymentsSchemaConstants.StatusMaxLength);
        builder.Property(x => x.Reason).HasMaxLength(PaymentsSchemaConstants.RefundReasonMaxLength);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql(PaymentsSchemaConstants.NowDefaultValueExpression);
    }
}
