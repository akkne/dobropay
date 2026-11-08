namespace Dobropay.DeveloperApi.Services;

public interface IAuthSessionResolver
{
    Task<Guid?> ResolveAccountIdAsync(string token, CancellationToken cancellationToken);
}
