using System.Security.Cryptography;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.DeveloperApi.Models;
using Dobropay.DeveloperApi.Persistence;

namespace Dobropay.DeveloperApi.Services;

public sealed class DeveloperService(
    IMerchantRepository repository,
    IHashProvider hashProvider,
    DeveloperApiOptions options) : IDeveloperService
{
    public async Task<DeveloperOperationResult<CreateShopResponse>> CreateShopAsync(
        Guid accountId,
        CreateShopRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Name))
        {
            return DeveloperOperationResult<CreateShopResponse>.Failure(
                400,
                ErrorCodes.InvalidPayload ?? "invalid_payload",
                "Shop name cannot be empty.");
        }

        var shopId = Guid.NewGuid();
        var shop = new Shop
        {
            Id = shopId,
            Name = request.Name.Trim()
        };

        var membership = new ShopMembership
        {
            ShopId = shopId,
            AccountId = accountId,
            Role = DomainConstants.OwnerRole
        };

        var now = DateTimeOffset.UtcNow;
        var profile = new ShopOnboardingProfile
        {
            ShopId = shopId,
            VerificationStatus = VerificationStatuses.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };

        var rawKey = GenerateRawKey(DomainConstants.TestApiKeyPrefix);
        var defaultKey = new ApiCredential
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            KeyHash = hashProvider.ComputeSha256Hex(rawKey),
            Name = "Default Test Key",
            Mode = DomainConstants.ApiKeyModeTest,
            KeyPrefix = BuildKeyPrefix(rawKey),
            CreatedAt = now
        };
        var keyDto = new ApiKeyCreatedDto(
            defaultKey.Id,
            defaultKey.Name,
            defaultKey.Mode,
            rawKey,
            defaultKey.KeyPrefix,
            BuildKeyMask(rawKey),
            defaultKey.CreatedAt);

        await repository.CreateShopWithDefaultsAsync(shop, membership, profile, defaultKey, cancellationToken);

        var onboardingDto = MapToOnboardingDto(profile);
        return DeveloperOperationResult<CreateShopResponse>.Success(
            new CreateShopResponse(shop.Id, shop.Name, membership.Role, onboardingDto, keyDto),
            201);
    }

    public async Task<IReadOnlyList<ShopListItemResponse>> ListShopsAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        return await repository.FindShopsForAccountAsync(accountId, cancellationToken);
    }

    public async Task<DeveloperOperationResult<ShopOnboardingDto>> GetOnboardingAsync(
        Guid accountId,
        Guid shopId,
        CancellationToken cancellationToken)
    {
        var membership = await repository.GetMembershipAsync(shopId, accountId, cancellationToken);
        if (membership is null)
        {
            return DeveloperOperationResult<ShopOnboardingDto>.Failure(
                403,
                ErrorCodes.Forbidden,
                "Account is not a member of this shop.");
        }

        if (!CanViewShop(membership.Role))
        {
            return DeveloperOperationResult<ShopOnboardingDto>.Failure(
                403,
                ErrorCodes.Forbidden,
                "Insufficient permissions.");
        }

        var profile = await repository.GetOnboardingProfileAsync(shopId, cancellationToken);
        if (profile is null)
        {
            profile = new ShopOnboardingProfile
            {
                ShopId = shopId,
                VerificationStatus = VerificationStatuses.Draft,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await repository.SaveOnboardingProfileAsync(profile, cancellationToken);
        }

        return DeveloperOperationResult<ShopOnboardingDto>.Success(MapToOnboardingDto(profile));
    }

    public async Task<DeveloperOperationResult<ShopOnboardingDto>> UpdateOnboardingAsync(
        Guid accountId,
        Guid shopId,
        UpdateOnboardingRequest request,
        CancellationToken cancellationToken)
    {
        var membership = await repository.GetMembershipAsync(shopId, accountId, cancellationToken);
        if (membership is null)
        {
            return DeveloperOperationResult<ShopOnboardingDto>.Failure(
                403,
                ErrorCodes.Forbidden,
                "Account is not a member of this shop.");
        }

        if (!CanManageShop(membership.Role))
        {
            return DeveloperOperationResult<ShopOnboardingDto>.Failure(
                403,
                ErrorCodes.Forbidden,
                "Role does not have permission to modify onboarding profile.");
        }

        var profile = await repository.GetOnboardingProfileAsync(shopId, cancellationToken);
        if (profile is null)
        {
            profile = new ShopOnboardingProfile
            {
                ShopId = shopId,
                VerificationStatus = VerificationStatuses.Draft,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
        }

        if (profile.VerificationStatus != VerificationStatuses.Draft &&
            profile.VerificationStatus != VerificationStatuses.Rejected)
        {
            return DeveloperOperationResult<ShopOnboardingDto>.Failure(
                400,
                ErrorCodes.InvalidOnboardingProfile,
                "Onboarding profile can only be modified in draft or rejected status.");
        }

        profile.Inn = request.Inn?.Trim();
        profile.Ogrn = request.Ogrn?.Trim();
        profile.LegalName = request.LegalName?.Trim();
        profile.BankBic = request.BankBic?.Trim();
        profile.BankAccount = request.BankAccount?.Trim();
        profile.WebsiteUrl = request.WebsiteUrl?.Trim();
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await repository.SaveOnboardingProfileAsync(profile, cancellationToken);
        return DeveloperOperationResult<ShopOnboardingDto>.Success(MapToOnboardingDto(profile));
    }

    public async Task<DeveloperOperationResult<ShopOnboardingDto>> SubmitOnboardingAsync(
        Guid accountId,
        Guid shopId,
        SubmitOnboardingRequest? request,
        CancellationToken cancellationToken)
    {
        var membership = await repository.GetMembershipAsync(shopId, accountId, cancellationToken);
        if (membership is null)
        {
            return DeveloperOperationResult<ShopOnboardingDto>.Failure(
                403,
                ErrorCodes.Forbidden,
                "Account is not a member of this shop.");
        }

        if (!CanManageShop(membership.Role))
        {
            return DeveloperOperationResult<ShopOnboardingDto>.Failure(
                403,
                ErrorCodes.Forbidden,
                "Role does not have permission to modify onboarding profile.");
        }

        var profile = await repository.GetOnboardingProfileAsync(shopId, cancellationToken);
        if (profile is null)
        {
            return DeveloperOperationResult<ShopOnboardingDto>.Failure(
                400,
                ErrorCodes.InvalidOnboardingProfile,
                "Onboarding profile does not exist.");
        }

        if (profile.VerificationStatus != VerificationStatuses.Draft &&
            profile.VerificationStatus != VerificationStatuses.Rejected)
        {
            return DeveloperOperationResult<ShopOnboardingDto>.Failure(
                400,
                ErrorCodes.InvalidOnboardingProfile,
                "Onboarding profile can only be submitted from draft or rejected status.");
        }

        var validationError = ValidateOnboardingFields(profile);
        if (validationError is not null)
        {
            return DeveloperOperationResult<ShopOnboardingDto>.Failure(
                400,
                ErrorCodes.InvalidOnboardingProfile,
                validationError);
        }

        if (options.SandboxEnabled && request?.AutoApprove == true)
        {
            profile.VerificationStatus = VerificationStatuses.Approved;
        }
        else
        {
            profile.VerificationStatus = VerificationStatuses.UnderReview;
        }

        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);

        return DeveloperOperationResult<ShopOnboardingDto>.Success(MapToOnboardingDto(profile));
    }

    public async Task<DeveloperOperationResult<ShopOnboardingDto>> ApproveOnboardingAsync(
        Guid shopId,
        CancellationToken cancellationToken)
    {
        var profile = await repository.GetOnboardingProfileAsync(shopId, cancellationToken);
        if (profile is null)
        {
            return DeveloperOperationResult<ShopOnboardingDto>.Failure(
                404,
                ErrorCodes.ShopNotFound,
                "Shop onboarding profile not found.");
        }

        profile.VerificationStatus = VerificationStatuses.Approved;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);

        return DeveloperOperationResult<ShopOnboardingDto>.Success(MapToOnboardingDto(profile));
    }

    public async Task<DeveloperOperationResult<ApiKeyCreatedDto>> CreateApiKeyAsync(
        Guid accountId,
        Guid shopId,
        CreateApiKeyRequest request,
        CancellationToken cancellationToken)
    {
        var membership = await repository.GetMembershipAsync(shopId, accountId, cancellationToken);
        if (membership is null)
        {
            return DeveloperOperationResult<ApiKeyCreatedDto>.Failure(
                403,
                ErrorCodes.Forbidden,
                "Account is not a member of this shop.");
        }

        if (!CanManageKeys(membership.Role))
        {
            return DeveloperOperationResult<ApiKeyCreatedDto>.Failure(
                403,
                ErrorCodes.Forbidden,
                "Role does not have permission to manage API keys.");
        }

        if (request.Mode != DomainConstants.ApiKeyModeTest &&
            request.Mode != DomainConstants.ApiKeyModeLive)
        {
            return DeveloperOperationResult<ApiKeyCreatedDto>.Failure(
                400,
                ErrorCodes.InvalidKeyMode,
                "Key mode must be either 'test' or 'live'.");
        }

        if (request.Mode == DomainConstants.ApiKeyModeLive)
        {
            var profile = await repository.GetOnboardingProfileAsync(shopId, cancellationToken);
            if (profile is null || profile.VerificationStatus != VerificationStatuses.Approved)
            {
                return DeveloperOperationResult<ApiKeyCreatedDto>.Failure(
                    403,
                    ErrorCodes.OnboardingNotApproved,
                    "Live API keys can only be issued after shop onboarding is approved.");
            }
        }

        var prefix = request.Mode == DomainConstants.ApiKeyModeLive
            ? DomainConstants.LiveApiKeyPrefix
            : DomainConstants.TestApiKeyPrefix;

        var rawKey = GenerateRawKey(prefix);
        var keyHash = hashProvider.ComputeSha256Hex(rawKey);
        var keyPrefix = BuildKeyPrefix(rawKey);
        var keyMask = BuildKeyMask(rawKey);

        var credential = new ApiCredential
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            KeyHash = keyHash,
            Name = string.IsNullOrWhiteSpace(request.Name) ? $"{request.Mode} Key" : request.Name.Trim(),
            Mode = request.Mode,
            KeyPrefix = keyPrefix,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await repository.AddApiKeyAsync(credential, cancellationToken);

        var keyDto = new ApiKeyCreatedDto(
            credential.Id,
            credential.Name,
            credential.Mode,
            rawKey,
            credential.KeyPrefix,
            keyMask,
            credential.CreatedAt);

        return DeveloperOperationResult<ApiKeyCreatedDto>.Success(keyDto, 201);
    }

    public async Task<DeveloperOperationResult<IReadOnlyList<ApiKeyDto>>> ListApiKeysAsync(
        Guid accountId,
        Guid shopId,
        CancellationToken cancellationToken)
    {
        var membership = await repository.GetMembershipAsync(shopId, accountId, cancellationToken);
        if (membership is null)
        {
            return DeveloperOperationResult<IReadOnlyList<ApiKeyDto>>.Failure(
                403,
                ErrorCodes.Forbidden,
                "Account is not a member of this shop.");
        }

        if (!CanViewShop(membership.Role))
        {
            return DeveloperOperationResult<IReadOnlyList<ApiKeyDto>>.Failure(
                403,
                ErrorCodes.Forbidden,
                "Insufficient permissions.");
        }

        var credentials = await repository.GetActiveApiKeysAsync(shopId, cancellationToken);
        var dtos = credentials.Select(c => new ApiKeyDto(
            c.Id,
            c.Name,
            c.Mode,
            c.KeyPrefix,
            c.KeyPrefix,
            c.CreatedAt)).ToList();

        return DeveloperOperationResult<IReadOnlyList<ApiKeyDto>>.Success(dtos);
    }

    public async Task<DeveloperOperationResult<bool>> RevokeApiKeyAsync(
        Guid accountId,
        Guid shopId,
        Guid keyId,
        CancellationToken cancellationToken)
    {
        var membership = await repository.GetMembershipAsync(shopId, accountId, cancellationToken);
        if (membership is null)
        {
            return DeveloperOperationResult<bool>.Failure(
                403,
                ErrorCodes.Forbidden,
                "Account is not a member of this shop.");
        }

        if (!CanManageKeys(membership.Role))
        {
            return DeveloperOperationResult<bool>.Failure(
                403,
                ErrorCodes.Forbidden,
                "Role does not have permission to manage API keys.");
        }

        var key = await repository.GetActiveApiKeyByIdAsync(shopId, keyId, cancellationToken);
        if (key is null)
        {
            return DeveloperOperationResult<bool>.Failure(
                404,
                ErrorCodes.KeyNotFound,
                "API key not found.");
        }

        key.RevokedAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);

        return DeveloperOperationResult<bool>.Success(true, 200);
    }

    private static bool CanManageShop(string role) =>
        role is DomainConstants.OwnerRole or DomainConstants.AdminRole;

    private static bool CanManageKeys(string role) =>
        role is DomainConstants.OwnerRole or DomainConstants.AdminRole or DomainConstants.DeveloperRole;

    private static bool CanViewShop(string role) =>
        role is DomainConstants.OwnerRole or DomainConstants.AdminRole or DomainConstants.DeveloperRole or DomainConstants.AnalystRole;

    private static string? ValidateOnboardingFields(ShopOnboardingProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Inn) ||
            !(profile.Inn.Length == 10 || profile.Inn.Length == 12) ||
            !profile.Inn.All(char.IsDigit))
        {
            return "INN must be 10 or 12 digits.";
        }

        if (string.IsNullOrWhiteSpace(profile.Ogrn) ||
            !(profile.Ogrn.Length == 13 || profile.Ogrn.Length == 15) ||
            !profile.Ogrn.All(char.IsDigit))
        {
            return "OGRN must be 13 or 15 digits.";
        }

        if (string.IsNullOrWhiteSpace(profile.LegalName) || profile.LegalName.Trim().Length < 2)
        {
            return "Legal name cannot be empty.";
        }

        if (string.IsNullOrWhiteSpace(profile.BankBic) ||
            profile.BankBic.Length != 9 ||
            !profile.BankBic.All(char.IsDigit))
        {
            return "Bank BIC must be 9 digits.";
        }

        if (string.IsNullOrWhiteSpace(profile.BankAccount) ||
            profile.BankAccount.Length < 20 ||
            profile.BankAccount.Length > 34 ||
            !profile.BankAccount.All(char.IsDigit))
        {
            return "Bank account must be between 20 and 34 digits.";
        }

        if (string.IsNullOrWhiteSpace(profile.WebsiteUrl) ||
            !Uri.TryCreate(profile.WebsiteUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return "Website URL must be a valid HTTP or HTTPS URL.";
        }

        return null;
    }

    private static string GenerateRawKey(string prefix)
    {
        var randomBytes = RandomNumberGenerator.GetBytes(24);
        return $"{prefix}{Convert.ToHexString(randomBytes).ToLowerInvariant()}";
    }

    private static string BuildKeyPrefix(string rawKey) =>
        rawKey.Length >= 12 ? $"{rawKey[..12]}..." : $"{rawKey}...";

    private static string BuildKeyMask(string rawKey) =>
        rawKey.Length >= 16 ? $"{rawKey[..12]}...{rawKey[^4..]}" : $"{rawKey}...";

    private static ShopOnboardingDto MapToOnboardingDto(ShopOnboardingProfile profile) =>
        new(
            profile.ShopId,
            profile.Inn,
            profile.Ogrn,
            profile.LegalName,
            profile.BankBic,
            profile.BankAccount,
            profile.WebsiteUrl,
            profile.VerificationStatus,
            profile.CreatedAt,
            profile.UpdatedAt);
}
