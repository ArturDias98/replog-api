using Microsoft.Extensions.Options;
using replog_api.Settings;
using replog_application;
using replog_application.Interfaces;
using replog_domain.Entities;
using replog_shared.Models.Responses;

namespace replog_api.Auth;

public class AuthService(
    IGoogleTokenValidator googleValidator,
    IUserRepository userRepository,
    ITokenService tokenService,
    IOptions<JwtSettings> jwtSettings
) : IAuthService
{
    private readonly JwtSettings _jwt = jwtSettings.Value;

    public async Task<Result<AuthResponse>> LoginAsync(string googleIdToken)
    {
        var googleUser = await googleValidator.ValidateAsync(googleIdToken);
        if (googleUser == null)
            return Result<AuthResponse>.Failure("invalid_google_token", "Invalid Google ID token.");

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
            ExpiresAt = now.AddDays(_jwt.RefreshTokenExpirationDays)
        };
        user.RefreshTokens.Add(tokenEntry);

        await userRepository.UpsertAsync(user);

        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, user.DisplayName, user.AvatarUrl);

        return Result<AuthResponse>.Success(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = now.AddMinutes(_jwt.AccessTokenExpirationMinutes)
        });
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(string accessToken, string refreshToken)
    {
        var userId = tokenService.GetUserIdFromExpiredToken(accessToken);
        if (userId == null)
            return Result<AuthResponse>.Failure("invalid_access_token", "Invalid access token.");

        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
            return Result<AuthResponse>.Failure("user_not_found", "User not found.");

        var providedHash = tokenService.HashToken(refreshToken);
        var matchingToken = user.RefreshTokens.Find(rt => rt.TokenHash == providedHash);

        if (matchingToken == null)
            return Result<AuthResponse>.Failure("invalid_refresh_token", "Invalid refresh token.");

        if (matchingToken.ExpiresAt < DateTime.UtcNow)
            return Result<AuthResponse>.Failure("token_expired", "Refresh token has expired.");

        var now = DateTime.UtcNow;
        var newRefreshToken = tokenService.GenerateRefreshToken();
        var newEntry = new RefreshTokenEntry
        {
            TokenHash = tokenService.HashToken(newRefreshToken),
            ExpiresAt = now.AddDays(_jwt.RefreshTokenExpirationDays)
        };

        await userRepository.ReplaceRefreshTokenAsync(userId, providedHash, newEntry);

        var newAccessToken = tokenService.GenerateAccessToken(user.Id, user.Email, user.DisplayName, user.AvatarUrl);

        return Result<AuthResponse>.Success(new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = now.AddMinutes(_jwt.AccessTokenExpirationMinutes)
        });
    }
}
