namespace replog_api.Settings;

public class JwtSettings
{
    public required string Secret { get; set; }
    public string Issuer { get; set; } = "replog-api";
    public string Audience { get; set; } = "replog-client";
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 30;
}
