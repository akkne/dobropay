using Microsoft.EntityFrameworkCore;

namespace Dobropay.DeveloperApi.Persistence;

public sealed class MerchantDbContext(DbContextOptions<MerchantDbContext> options) : DbContext(options)
{
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<ApiCredential> ApiCredentials => Set<ApiCredential>();
    public DbSet<ShopMembership> ShopMemberships => Set<ShopMembership>();
    public DbSet<ShopOnboardingProfile> ShopOnboardingProfiles => Set<ShopOnboardingProfile>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Shop>(entity =>
        {
            entity.ToTable(MerchantSchemaConstants.ShopsTable).HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName(MerchantSchemaConstants.IdColumn);
            entity.Property(x => x.Name)
                .HasColumnName(MerchantSchemaConstants.NameColumn)
                .HasMaxLength(MerchantSchemaConstants.MaxShopNameLength);
        });

        model.Entity<ApiCredential>(entity =>
        {
            entity.ToTable(MerchantSchemaConstants.ApiCredentialsTable).HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName(MerchantSchemaConstants.IdColumn);
            entity.Property(x => x.ShopId).HasColumnName(MerchantSchemaConstants.ShopIdColumn);
            entity.Property(x => x.KeyHash)
                .HasColumnName(MerchantSchemaConstants.KeyHashColumn)
                .HasMaxLength(MerchantSchemaConstants.KeyHashLength);
            entity.Property(x => x.Name)
                .HasColumnName(MerchantSchemaConstants.NameColumn)
                .HasMaxLength(MerchantSchemaConstants.MaxKeyNameLength);
            entity.Property(x => x.Mode)
                .HasColumnName(MerchantSchemaConstants.ModeColumn)
                .HasMaxLength(MerchantSchemaConstants.MaxKeyModeLength);
            entity.Property(x => x.KeyPrefix)
                .HasColumnName(MerchantSchemaConstants.KeyPrefixColumn)
                .HasMaxLength(MerchantSchemaConstants.MaxKeyPrefixLength);
            entity.Property(x => x.CreatedAt).HasColumnName(MerchantSchemaConstants.CreatedAtColumn);
            entity.Property(x => x.RevokedAt).HasColumnName(MerchantSchemaConstants.RevokedAtColumn);
            entity.HasIndex(x => x.KeyHash).IsUnique();
            entity.HasIndex(x => new { x.ShopId, x.Id }).IsUnique();
        });

        model.Entity<ShopMembership>(entity =>
        {
            entity.ToTable(MerchantSchemaConstants.ShopMembershipsTable).HasKey(x => new { x.ShopId, x.AccountId });
            entity.Property(x => x.ShopId).HasColumnName(MerchantSchemaConstants.ShopIdColumn);
            entity.Property(x => x.AccountId).HasColumnName(MerchantSchemaConstants.AccountIdColumn);
            entity.Property(x => x.Role)
                .HasColumnName(MerchantSchemaConstants.RoleColumn)
                .HasMaxLength(MerchantSchemaConstants.MaxRoleLength);
        });

        model.Entity<ShopOnboardingProfile>(entity =>
        {
            entity.ToTable(MerchantSchemaConstants.ShopOnboardingProfilesTable).HasKey(x => x.ShopId);
            entity.Property(x => x.ShopId).HasColumnName(MerchantSchemaConstants.ShopIdColumn);
            entity.Property(x => x.Inn)
                .HasColumnName(MerchantSchemaConstants.InnColumn)
                .HasMaxLength(MerchantSchemaConstants.MaxInnLength);
            entity.Property(x => x.Ogrn)
                .HasColumnName(MerchantSchemaConstants.OgrnColumn)
                .HasMaxLength(MerchantSchemaConstants.MaxOgrnLength);
            entity.Property(x => x.LegalName)
                .HasColumnName(MerchantSchemaConstants.LegalNameColumn)
                .HasMaxLength(MerchantSchemaConstants.MaxLegalNameLength);
            entity.Property(x => x.BankBic)
                .HasColumnName(MerchantSchemaConstants.BankBicColumn)
                .HasMaxLength(MerchantSchemaConstants.MaxBankBicLength);
            entity.Property(x => x.BankAccount)
                .HasColumnName(MerchantSchemaConstants.BankAccountColumn)
                .HasMaxLength(MerchantSchemaConstants.MaxBankAccountLength);
            entity.Property(x => x.WebsiteUrl)
                .HasColumnName(MerchantSchemaConstants.WebsiteUrlColumn)
                .HasMaxLength(MerchantSchemaConstants.MaxWebsiteUrlLength);
            entity.Property(x => x.VerificationStatus)
                .HasColumnName(MerchantSchemaConstants.VerificationStatusColumn)
                .HasMaxLength(MerchantSchemaConstants.MaxVerificationStatusLength);
            entity.Property(x => x.CreatedAt).HasColumnName(MerchantSchemaConstants.CreatedAtColumn);
            entity.Property(x => x.UpdatedAt).HasColumnName(MerchantSchemaConstants.UpdatedAtColumn);
        });
    }
}
