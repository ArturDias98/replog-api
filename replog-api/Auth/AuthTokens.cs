namespace replog_api.Auth;

public record AuthTokens(string AccessToken, string RefreshToken, DateTime ExpiresAt);
