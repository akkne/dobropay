using Dobropay.Auth.Models;

namespace Dobropay.Auth.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<ResolveSessionResponse?> ResolveSessionAsync(ResolveSessionRequest request, CancellationToken cancellationToken);
}
