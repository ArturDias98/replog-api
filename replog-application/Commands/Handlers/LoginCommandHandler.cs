using replog_application.Interfaces;
using replog_domain.Entities;
using replog_shared.Models.Responses;

namespace replog_application.Commands.Handlers;

public class LoginCommandHandler(
    IGoogleTokenValidator googleValidator,
    IUserRepository userRepository,
    ITokenService tokenService
) : ICommandHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> HandleAsync(LoginCommand command)
    {
        var googleUser = await googleValidator.ValidateAsync(command.GoogleIdToken)
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

        // Clean up expired refresh tokens
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
}
