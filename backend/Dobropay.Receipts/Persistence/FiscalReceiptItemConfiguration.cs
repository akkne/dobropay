using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dobropay.Receipts.Persistence;

public sealed class FiscalReceiptItemConfiguration : IEntityTypeConfiguration<FiscalReceiptItem>
{
    public void Configure(EntityTypeBuilder<FiscalReceiptItem> builder)
    {
        builder.ToTable(ReceiptsSchemaConstants.FiscalReceiptItemsTable).HasKey(x => x.Id);
        builder.HasIndex(x => x.ReceiptId);

        builder.Property(x => x.Description).HasMaxLength(ReceiptsSchemaConstants.ItemDescriptionMaxLength);
        builder.Property(x => x.Quantity).HasMaxLength(ReceiptsSchemaConstants.ItemQuantityMaxLength);
        builder.Property(x => x.PaymentMode).HasMaxLength(ReceiptsSchemaConstants.PaymentModeMaxLength);
        builder.Property(x => x.PaymentSubject).HasMaxLength(ReceiptsSchemaConstants.PaymentSubjectMaxLength);
    }
}
