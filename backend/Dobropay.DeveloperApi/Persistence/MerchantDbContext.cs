using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dobropay.DeveloperApi.Persistence;

public sealed class Shop
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

public sealed class ApiCredential
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string KeyHash { get; set; } = "";
}

public sealed class ShopMembership
{
    public Guid ShopId { get; set; }
    public Guid AccountId { get; set; }
    public string Role { get; set; } = "owner";
}

public sealed class MerchantDbContext(DbContextOptions<MerchantDbContext> options) : DbContext(options)
{
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<ApiCredential> ApiCredentials => Set<ApiCredential>();
    public DbSet<ShopMembership> ShopMemberships => Set<ShopMembership>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Shop>().ToTable("shops").HasKey(x => x.Id);
        model.Entity<Shop>().Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
        model.Entity<Shop>().Property(x => x.Id).HasColumnName("id");

        model.Entity<ApiCredential>().ToTable("api_credentials").HasKey(x => x.Id);
        model.Entity<ApiCredential>().Property(x => x.Id).HasColumnName("id");
        model.Entity<ApiCredential>().Property(x => x.ShopId).HasColumnName("shop_id");
        model.Entity<ApiCredential>().Property(x => x.KeyHash).HasColumnName("key_hash").HasMaxLength(64);
        model.Entity<ApiCredential>().HasIndex(x => x.KeyHash).IsUnique();
        model.Entity<ApiCredential>().HasIndex(x => new { x.ShopId, x.Id }).IsUnique();

        model.Entity<ShopMembership>().ToTable("shop_memberships").HasKey(x => new { x.ShopId, x.AccountId });
        model.Entity<ShopMembership>().Property(x => x.ShopId).HasColumnName("shop_id");
        model.Entity<ShopMembership>().Property(x => x.AccountId).HasColumnName("account_id");
        model.Entity<ShopMembership>().Property(x => x.Role).HasColumnName("role").HasMaxLength(32);
    }
}

public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<MerchantDbContext>
{
    public MerchantDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<MerchantDbContext>()
        .UseNpgsql("Host=localhost;Database=merchant_dev_db;Username=postgres;Password=local-postgres-password").Options);
}
