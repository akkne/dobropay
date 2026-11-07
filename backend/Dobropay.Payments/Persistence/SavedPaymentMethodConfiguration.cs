using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dobropay.Payments.Persistence;

public sealed class SavedPaymentMethodConfiguration : IEntityTypeConfiguration<SavedPaymentMethod>
{
    public void Configure(EntityTypeBuilder<SavedPaymentMethod> builder)
    {
        builder.ToTable(PaymentsSchemaConstants.SavedPaymentMethodsTable).HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.ShopId, x.Id });
        builder.HasIndex(x => x.ProviderToken).IsUnique();
        builder.HasIndex(x => new { x.ShopId, x.SavedAt });
        builder.Property(x => x.Type).HasMaxLength(PaymentsSchemaConstants.PaymentMethodTypeMaxLength);
        builder.Property(x => x.CardFirst6).HasMaxLength(PaymentsSchemaConstants.CardFirst6MaxLength);
        builder.Property(x => x.CardLast4).HasMaxLength(PaymentsSchemaConstants.CardLast4MaxLength);
        builder.Property(x => x.CardBrand).HasMaxLength(PaymentsSchemaConstants.CardBrandMaxLength);
        builder.Property(x => x.ProviderToken).HasMaxLength(PaymentsSchemaConstants.ProviderTokenMaxLength);
        builder.Property(x => x.SavedAt).HasDefaultValueSql(PaymentsSchemaConstants.NowDefaultValueExpression);
    }
}
