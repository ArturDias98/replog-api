namespace replog_api.Auth;

public interface ITokenService
{
    string GenerateAccessToken(string userId, string email, string displayName, string? avatarUrl);
    string GenerateRefreshToken();
    string HashToken(string token);
    string? GetUserIdFromExpiredToken(string token);
}
