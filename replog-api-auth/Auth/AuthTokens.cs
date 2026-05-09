namespace replog_api_auth.Auth;

public record AuthTokens(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string UserId,
    string Email,
    string DisplayName,
    string? AvatarUrl);
