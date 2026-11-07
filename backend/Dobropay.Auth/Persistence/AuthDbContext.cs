using Microsoft.EntityFrameworkCore;

namespace Dobropay.Auth.Persistence;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<StaffSession> StaffSessions => Set<StaffSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable(AuthSchemaConstants.AccountsTable).HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName(AuthSchemaConstants.IdColumn);
            entity.Property(x => x.Email).HasColumnName(AuthSchemaConstants.EmailColumn);
            entity.Property(x => x.PasswordSalt).HasColumnName(AuthSchemaConstants.PasswordSaltColumn);
            entity.Property(x => x.PasswordHash).HasColumnName(AuthSchemaConstants.PasswordHashColumn);
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<StaffSession>(entity =>
        {
            entity.ToTable(AuthSchemaConstants.StaffSessionsTable).HasKey(x => x.TokenHash);
            entity.Property(x => x.TokenHash).HasColumnName(AuthSchemaConstants.TokenHashColumn);
            entity.Property(x => x.AccountId).HasColumnName(AuthSchemaConstants.AccountIdColumn);
            entity.Property(x => x.ExpiresAt).HasColumnName(AuthSchemaConstants.ExpiresAtColumn);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId);
        });
    }
}
