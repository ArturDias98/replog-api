using replog_application.Interfaces;
using replog_domain.Entities;
using replog_shared.Models.Responses;

namespace replog_api.Auth;

public class AuthService(
    IGoogleTokenValidator googleValidator,
    IUserRepository userRepository,
    ITokenService tokenService
) : IAuthService
{
    public async Task<AuthResponse> LoginAsync(string googleIdToken)
    {
        var googleUser = await googleValidator.ValidateAsync(googleIdToken)
            ?? throw new UnauthorizedAccessException("Invalid Google ID token.");

        var now = DateTime.UtcNow;
        var existingUser = await userRepository.GetByIdAsync(googleUser.Subject);

        var user = existingUser ?? new UserEntity
        {
            Id = googleUser.Subject,
            Email = googleUser.Email,
            DisplayName = googleUser.Name,
            AvatarUrl = googleUser.Picture,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (existingUser != null)
        {
            user.Email = googleUser.Email;
            user.DisplayName = googleUser.Name;
            user.AvatarUrl = googleUser.Picture;
            user.UpdatedAt = now;
        }

        user.RefreshTokens.RemoveAll(rt => rt.ExpiresAt < now);

        var refreshToken = tokenService.GenerateRefreshToken();
        var tokenEntry = new RefreshTokenEntry
        {
            TokenHash = tokenService.HashToken(refreshToken),
            ExpiresAt = now.AddDays(30)
        };
        user.RefreshTokens.Add(tokenEntry);

        await userRepository.UpsertAsync(user);

        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, user.DisplayName, user.AvatarUrl);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = now.AddMinutes(15)
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(string accessToken, string refreshToken)
    {
        var userId = tokenService.GetUserIdFromExpiredToken(accessToken)
            ?? throw new UnauthorizedAccessException("Invalid access token.");

        var user = await userRepository.GetByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("User not found.");

        var providedHash = tokenService.HashToken(refreshToken);
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

        var newAccessToken = tokenService.GenerateAccessToken(user.Id, user.Email, user.DisplayName, user.AvatarUrl);

        return new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = now.AddMinutes(15)
        };
    }
}
