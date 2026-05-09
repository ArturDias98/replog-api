using replog_application;

namespace replog_api_auth.Auth;

public interface IAuthService
{
    Task<Result<AuthTokens>> LoginAsync(string googleIdToken, CancellationToken cancellationToken = default);
    Task<Result<AuthTokens>> RefreshTokenAsync(string accessToken, string refreshToken, CancellationToken cancellationToken = default);
}
