using System.Security.Cryptography;
using System.Text;
using Dobropay.Auth.Persistence;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.DeveloperApi.Persistence;
using Dobropay.Notifications.Persistence;
using Dobropay.Payments.Persistence;
using Dobropay.Receipts.Persistence;
using Dobropay.Setup.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Dobropay.Setup.Services;

public sealed class EfCoreDatabaseSeeder(
    SetupOptions options,
    IHashProvider hashProvider) : IDatabaseSeeder
{
    private const int Pbkdf2Iterations = 210000;
    private const int HashByteLength = 32;

    public async Task MigrateAndSeedAllAsync(CancellationToken cancellationToken)
    {
        await MigrateAndSeedAuthAsync(cancellationToken);
        await MigrateAndSeedMerchantAsync(cancellationToken);
        await MigratePaymentsAsync(cancellationToken);
        await MigrateAndSeedNotificationsAsync(cancellationToken);
        await MigrateReceiptsAsync(cancellationToken);
    }

    private async Task MigrateAndSeedAuthAsync(CancellationToken cancellationToken)
    {
        var contextOptions = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(options.AuthDbConnectionString)
            .Options;

        await using var dbContext = new AuthDbContext(contextOptions);
        await dbContext.Database.MigrateAsync(cancellationToken);

        var saltBytes = SHA256.HashData(Encoding.UTF8.GetBytes(options.DemoStaffSaltSeed));
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            options.DemoStaffPassword,
            saltBytes,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            HashByteLength);

        var saltBase64 = Convert.ToBase64String(saltBytes);
        var hashBase64 = Convert.ToBase64String(hashBytes);

        var account = await dbContext.Accounts
            .SingleOrDefaultAsync(x => x.Id == options.DemoStaffId, cancellationToken);
        if (account is null)
        {
            await dbContext.Accounts.AddAsync(
                new Account
                {
                    Id = options.DemoStaffId,
                    Email = options.DemoStaffEmail,
                    PasswordSalt = saltBase64,
                    PasswordHash = hashBase64
                },
                cancellationToken);
        }
        else
        {
            account.Email = options.DemoStaffEmail;
            account.PasswordSalt = saltBase64;
            account.PasswordHash = hashBase64;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task MigrateAndSeedMerchantAsync(CancellationToken cancellationToken)
    {
        var contextOptions = new DbContextOptionsBuilder<MerchantDbContext>()
            .UseNpgsql(options.MerchantDevDbConnectionString)
            .Options;

        await using var dbContext = new MerchantDbContext(contextOptions);
        await dbContext.Database.MigrateAsync(cancellationToken);

        foreach (var shopSpec in options.DemoShops)
        {
            var shop = await dbContext.Shops
                .SingleOrDefaultAsync(x => x.Id == shopSpec.ShopId, cancellationToken);
            if (shop is null)
            {
                await dbContext.Shops.AddAsync(
                    new Shop
                    {
                        Id = shopSpec.ShopId,
                        Name = shopSpec.ShopName
                    },
                    cancellationToken);
            }
            else
            {
                shop.Name = shopSpec.ShopName;
            }

            var keyHash = hashProvider.ComputeSha256Hex(shopSpec.RawApiKey);
            var credential = await dbContext.ApiCredentials
                .SingleOrDefaultAsync(x => x.Id == shopSpec.CredentialId, cancellationToken);
            if (credential is null)
            {
                await dbContext.ApiCredentials.AddAsync(
                    new ApiCredential
                    {
                        Id = shopSpec.CredentialId,
                        ShopId = shopSpec.ShopId,
                        KeyHash = keyHash,
                        Name = "Default Key",
                        Mode = DomainConstants.ApiKeyModeTest,
                        KeyPrefix = shopSpec.RawApiKey[..Math.Min(12, shopSpec.RawApiKey.Length)],
                        CreatedAt = DateTimeOffset.UtcNow
                    },
                    cancellationToken);
            }
            else
            {
                credential.ShopId = shopSpec.ShopId;
                credential.KeyHash = keyHash;
            }

            var profile = await dbContext.ShopOnboardingProfiles
                .SingleOrDefaultAsync(x => x.ShopId == shopSpec.ShopId, cancellationToken);
            if (profile is null)
            {
                await dbContext.ShopOnboardingProfiles.AddAsync(
                    new ShopOnboardingProfile
                    {
                        ShopId = shopSpec.ShopId,
                        Inn = "7701234567",
                        Ogrn = "1027700123456",
                        LegalName = $"{shopSpec.ShopName} LLC",
                        BankBic = "044525225",
                        BankAccount = "40702810400000000001",
                        WebsiteUrl = "https://example.com",
                        VerificationStatus = VerificationStatuses.Approved,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    cancellationToken);
            }

            var membershipExists = await dbContext.ShopMemberships
                .AnyAsync(
                    x => x.ShopId == shopSpec.ShopId && x.AccountId == options.DemoStaffId,
                    cancellationToken);
            if (!membershipExists)
            {
                await dbContext.ShopMemberships.AddAsync(
                    new ShopMembership
                    {
                        ShopId = shopSpec.ShopId,
                        AccountId = options.DemoStaffId,
                        Role = DomainConstants.OwnerRole
                    },
                    cancellationToken);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task MigratePaymentsAsync(CancellationToken cancellationToken)
    {
        var contextOptions = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseNpgsql(options.PaymentsDbConnectionString)
            .Options;

        await using var dbContext = new PaymentsDbContext(contextOptions);
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    private async Task MigrateAndSeedNotificationsAsync(CancellationToken cancellationToken)
    {
        var contextOptions = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseNpgsql(options.NotificationsDbConnectionString)
            .Options;

        await using var dbContext = new NotificationsDbContext(contextOptions);
        await dbContext.Database.MigrateAsync(cancellationToken);

        foreach (var shopSpec in options.DemoShops)
        {
            var subscription = await dbContext.WebhookSubscriptions
                .SingleOrDefaultAsync(x => x.Id == shopSpec.SubscriptionId, cancellationToken);
            if (subscription is null)
            {
                await dbContext.WebhookSubscriptions.AddAsync(
                    new WebhookSubscription
                    {
                        Id = shopSpec.SubscriptionId,
                        ShopId = shopSpec.ShopId,
                        Url = options.DemoWebhookUrl,
                        SigningSecret = options.WebhookSecret
                    },
                    cancellationToken);
            }
            else
            {
                subscription.ShopId = shopSpec.ShopId;
                subscription.Url = options.DemoWebhookUrl;
                subscription.SigningSecret = options.WebhookSecret;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task MigrateReceiptsAsync(CancellationToken cancellationToken)
    {
        var contextOptions = new DbContextOptionsBuilder<ReceiptsDbContext>()
            .UseNpgsql(options.ReceiptsDbConnectionString)
            .Options;

        await using var dbContext = new ReceiptsDbContext(contextOptions);
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
