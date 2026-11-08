using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dobropay.Receipts.Persistence;

public sealed class FiscalReceiptConfiguration : IEntityTypeConfiguration<FiscalReceipt>
{
    public void Configure(EntityTypeBuilder<FiscalReceipt> builder)
    {
        builder.ToTable(ReceiptsSchemaConstants.FiscalReceiptsTable).HasKey(x => x.Id);
        builder.HasIndex(x => x.ShopId);
        builder.HasIndex(x => x.PaymentId);
        builder.HasIndex(x => x.FiscalDocumentNumber);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.ShopId, x.CreatedAt });
        builder.HasIndex(x => new { x.PaymentId, x.RefundId });

        builder.Property(x => x.ReceiptType).HasMaxLength(ReceiptsSchemaConstants.ReceiptTypeMaxLength);
        builder.Property(x => x.Status).HasMaxLength(ReceiptsSchemaConstants.StatusMaxLength);
        builder.Property(x => x.Currency).HasMaxLength(ReceiptsSchemaConstants.CurrencyMaxLength);
        builder.Property(x => x.CustomerEmail).HasMaxLength(ReceiptsSchemaConstants.CustomerEmailMaxLength);
        builder.Property(x => x.CustomerPhone).HasMaxLength(ReceiptsSchemaConstants.CustomerPhoneMaxLength);
        builder.Property(x => x.FiscalStorageNumber).HasMaxLength(ReceiptsSchemaConstants.FiscalStorageNumberMaxLength);
        builder.Property(x => x.FiscalAttribute).HasMaxLength(ReceiptsSchemaConstants.FiscalAttributeMaxLength);
        builder.Property(x => x.FiscalSign).HasMaxLength(ReceiptsSchemaConstants.FiscalSignMaxLength);
        builder.Property(x => x.QrPayload).HasMaxLength(ReceiptsSchemaConstants.QrPayloadMaxLength);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql(ReceiptsSchemaConstants.NowDefaultValueExpression);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.Receipt)
            .HasForeignKey(x => x.ReceiptId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
