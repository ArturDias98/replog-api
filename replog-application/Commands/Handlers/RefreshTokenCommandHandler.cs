using replog_application.Interfaces;
using replog_domain.Entities;
using replog_shared.Models.Responses;

namespace replog_application.Commands.Handlers;

public class RefreshTokenCommandHandler(
    IUserRepository userRepository,
    ITokenService tokenService
) : ICommandHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<AuthResponse> HandleAsync(RefreshTokenCommand command)
    {
        var userId = tokenService.GetUserIdFromExpiredToken(command.AccessToken)
            ?? throw new UnauthorizedAccessException("Invalid access token.");

        var user = await userRepository.GetByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("User not found.");

        var providedHash = tokenService.HashToken(command.RefreshToken);
        var matchingToken = user.RefreshTokens.Find(rt => rt.TokenHash == providedHash);

        if (matchingToken == null)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        if (matchingToken.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token has expired.");

        var now = DateTime.UtcNow;
        var newRefreshToken = tokenService.GenerateRefreshToken();
        var newEntry = new RefreshTokenEntry
        {
            TokenHash = tokenService.HashToken(newRefreshToken),
            ExpiresAt = now.AddDays(30)
        };

        await userRepository.ReplaceRefreshTokenAsync(userId, providedHash, newEntry);

        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, user.DisplayName, user.AvatarUrl);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = now.AddMinutes(15)
        };
    }
}
