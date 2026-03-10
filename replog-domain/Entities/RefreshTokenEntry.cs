namespace replog_domain.Entities;

public class RefreshTokenEntry
{
    public required string TokenHash { get; set; }
    public required DateTime ExpiresAt { get; set; }
}
