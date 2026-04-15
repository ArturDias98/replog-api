using replog_application;

namespace replog_api.Auth;

public interface IAuthService
{
    Task<Result<AuthTokens>> LoginAsync(string googleIdToken, CancellationToken cancellationToken = default);
    Task<Result<AuthTokens>> RefreshTokenAsync(string accessToken, string refreshToken, CancellationToken cancellationToken = default);
}
