using replog_shared.Models.Responses;

namespace replog_api.Auth;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(string googleIdToken);
    Task<AuthResponse> RefreshTokenAsync(string accessToken, string refreshToken);
}
