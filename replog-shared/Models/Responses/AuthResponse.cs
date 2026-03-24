namespace replog_shared.Models.Responses;

public class AuthResponse
{
    public required DateTime ExpiresAt { get; set; }
    public required string UserId { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
}
