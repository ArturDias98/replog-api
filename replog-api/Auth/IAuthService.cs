using replog_application;
using replog_shared.Models.Responses;

namespace replog_api.Auth;

public interface IAuthService
{
    Task<Result<AuthResponse>> LoginAsync(string googleIdToken);
    Task<Result<AuthResponse>> RefreshTokenAsync(string accessToken, string refreshToken);
}
