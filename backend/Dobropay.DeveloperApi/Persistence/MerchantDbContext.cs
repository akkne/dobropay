using Microsoft.EntityFrameworkCore;

namespace Dobropay.DeveloperApi.Persistence;

public sealed class MerchantDbContext(DbContextOptions<MerchantDbContext> options) : DbContext(options)
{
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<ApiCredential> ApiCredentials => Set<ApiCredential>();
    public DbSet<ShopMembership> ShopMemberships => Set<ShopMembership>();

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
    }
}
