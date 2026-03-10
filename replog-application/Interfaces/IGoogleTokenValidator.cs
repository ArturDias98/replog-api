namespace replog_application.Interfaces;

public interface IGoogleTokenValidator
{
    Task<GoogleUserInfo?> ValidateAsync(string idToken);
}

public class GoogleUserInfo
{
    public required string Subject { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
    public string? Picture { get; set; }
}
