using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dobropay.Auth.Persistence;

public sealed class Account
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordSalt { get; set; } = "";
    public string PasswordHash { get; set; } = "";
}

public sealed class StaffSession
{
    public string TokenHash { get; set; } = "";
    public Guid AccountId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<StaffSession> StaffSessions => Set<StaffSession>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Account>().ToTable("accounts").HasKey(x => x.Id);
        b.Entity<Account>().HasIndex(x => x.Email).IsUnique();
        b.Entity<StaffSession>().ToTable("staff_sessions").HasKey(x => x.TokenHash);
        b.Entity<StaffSession>().HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId);
        foreach (var e in b.Model.GetEntityTypes())
            foreach (var p in e.GetProperties()) p.SetColumnName(System.Text.RegularExpressions.Regex.Replace(p.Name, "(?<!^)([A-Z])", "_$1").ToLowerInvariant());
    }
}

public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<AuthDbContext>()
        .UseNpgsql("Host=localhost;Database=auth_db;Username=postgres;Password=local-postgres-password").Options);
}
