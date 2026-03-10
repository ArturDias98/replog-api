namespace replog_application.Commands;

public class RefreshTokenCommand
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
}
