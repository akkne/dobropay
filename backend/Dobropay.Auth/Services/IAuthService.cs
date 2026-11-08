using Dobropay.Auth.Models;

namespace Dobropay.Auth.Services;

public interface IAuthService
{
    Task<RegisterResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthResponse?> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken);
    Task<bool> LogoutAsync(string token, CancellationToken cancellationToken);
    Task<AuthMeResponse?> GetMeAsync(string token, CancellationToken cancellationToken);
    Task<ResolveSessionResponse?> ResolveSessionAsync(ResolveSessionRequest request, CancellationToken cancellationToken);
}
