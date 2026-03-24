namespace replog_api.Auth;

public record AuthTokens(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string UserId,
    string Email,
    string DisplayName,
    string? AvatarUrl);
