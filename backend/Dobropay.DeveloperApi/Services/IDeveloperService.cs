using Dobropay.DeveloperApi.Models;

namespace Dobropay.DeveloperApi.Services;

public interface IDeveloperService
{
    Task<DeveloperOperationResult<CreateShopResponse>> CreateShopAsync(
        Guid accountId,
        CreateShopRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ShopListItemResponse>> ListShopsAsync(
        Guid accountId,
        CancellationToken cancellationToken);

    Task<DeveloperOperationResult<ShopOnboardingDto>> GetOnboardingAsync(
        Guid accountId,
        Guid shopId,
        CancellationToken cancellationToken);

    Task<DeveloperOperationResult<ShopOnboardingDto>> UpdateOnboardingAsync(
        Guid accountId,
        Guid shopId,
        UpdateOnboardingRequest request,
        CancellationToken cancellationToken);

    Task<DeveloperOperationResult<ShopOnboardingDto>> SubmitOnboardingAsync(
        Guid accountId,
        Guid shopId,
        SubmitOnboardingRequest? request,
        CancellationToken cancellationToken);

    Task<DeveloperOperationResult<ShopOnboardingDto>> ApproveOnboardingAsync(
        Guid shopId,
        CancellationToken cancellationToken);

    Task<DeveloperOperationResult<ApiKeyCreatedDto>> CreateApiKeyAsync(
        Guid accountId,
        Guid shopId,
        CreateApiKeyRequest request,
        CancellationToken cancellationToken);

    Task<DeveloperOperationResult<IReadOnlyList<ApiKeyDto>>> ListApiKeysAsync(
        Guid accountId,
        Guid shopId,
        CancellationToken cancellationToken);

    Task<DeveloperOperationResult<bool>> RevokeApiKeyAsync(
        Guid accountId,
        Guid shopId,
        Guid keyId,
        CancellationToken cancellationToken);
}
