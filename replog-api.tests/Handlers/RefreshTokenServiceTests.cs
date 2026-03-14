using Microsoft.Extensions.Options;
using NSubstitute;
using replog_api.Auth;
using replog_api.Settings;
using replog_application.Interfaces;
using replog_domain.Entities;

namespace replog_api.tests.Handlers;

public class RefreshTokenServiceTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly AuthService _service;

    public RefreshTokenServiceTests()
    {
        var jwtSettings = Options.Create(new JwtSettings { Secret = "test-secret", AccessTokenExpirationMinutes = 15, RefreshTokenExpirationDays = 30 });
        _service = new AuthService(Substitute.For<IGoogleTokenValidator>(), _userRepository, _tokenService, jwtSettings);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnNewTokens_WhenRefreshTokenIsValid()
    {
        var user = new UserEntity
        {
            Id = "user-123",
            Email = "user@example.com",
            DisplayName = "User",
            RefreshTokens =
            [
                new RefreshTokenEntry
                {
                    TokenHash = "stored-hash",
                    ExpiresAt = DateTime.UtcNow.AddDays(10)
                }
            ],
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _tokenService.GetUserIdFromExpiredToken("expired-access").Returns("user-123");
        _userRepository.GetByIdAsync("user-123").Returns(user);
        _tokenService.HashToken("valid-refresh").Returns("stored-hash");
        _tokenService.GenerateRefreshToken().Returns("new-refresh-token");
        _tokenService.HashToken("new-refresh-token").Returns("new-hash");
        _tokenService.GenerateAccessToken("user-123", "user@example.com", "User", Arg.Any<string?>()).Returns("new-access-token");

        var result = await _service.RefreshTokenAsync("expired-access", "valid-refresh");

        Assert.True(result.IsSuccess);
        Assert.Equal("new-access-token", result.Value!.AccessToken);
        Assert.Equal("new-refresh-token", result.Value.RefreshToken);
        Assert.True(result.Value.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnFailure_WhenAccessTokenIsInvalid()
    {
        _tokenService.GetUserIdFromExpiredToken("bad-token").Returns((string?)null);

        var result = await _service.RefreshTokenAsync("bad-token", "some-refresh");

        Assert.False(result.IsSuccess);
        Assert.Equal("invalid_access_token", result.ErrorCode);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnFailure_WhenUserNotFound()
    {
        _tokenService.GetUserIdFromExpiredToken("token").Returns("user-123");
        _userRepository.GetByIdAsync("user-123").Returns((UserEntity?)null);

        var result = await _service.RefreshTokenAsync("token", "refresh");

        Assert.False(result.IsSuccess);
        Assert.Equal("user_not_found", result.ErrorCode);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnFailure_WhenRefreshTokenIsExpired()
    {
        var user = new UserEntity
        {
            Id = "user-123",
            Email = "user@example.com",
            DisplayName = "User",
            RefreshTokens =
            [
                new RefreshTokenEntry
                {
                    TokenHash = "stored-hash",
                    ExpiresAt = DateTime.UtcNow.AddDays(-1)
                }
            ],
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _tokenService.GetUserIdFromExpiredToken("token").Returns("user-123");
        _userRepository.GetByIdAsync("user-123").Returns(user);
        _tokenService.HashToken("refresh").Returns("stored-hash");

        var result = await _service.RefreshTokenAsync("token", "refresh");

        Assert.False(result.IsSuccess);
        Assert.Equal("token_expired", result.ErrorCode);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnFailure_WhenRefreshTokenHashDoesNotMatch()
    {
        var user = new UserEntity
        {
            Id = "user-123",
            Email = "user@example.com",
            DisplayName = "User",
            RefreshTokens =
            [
                new RefreshTokenEntry
                {
                    TokenHash = "stored-hash",
                    ExpiresAt = DateTime.UtcNow.AddDays(10)
                }
            ],
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _tokenService.GetUserIdFromExpiredToken("token").Returns("user-123");
        _userRepository.GetByIdAsync("user-123").Returns(user);
        _tokenService.HashToken("wrong-refresh").Returns("wrong-hash");

        var result = await _service.RefreshTokenAsync("token", "wrong-refresh");

        Assert.False(result.IsSuccess);
        Assert.Equal("invalid_refresh_token", result.ErrorCode);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldCallReplaceRefreshToken_WhenTokenIsValid()
    {
        var user = new UserEntity
        {
            Id = "user-456",
            Email = "user@example.com",
            DisplayName = "User",
            RefreshTokens =
            [
                new RefreshTokenEntry
                {
                    TokenHash = "old-hash",
                    ExpiresAt = DateTime.UtcNow.AddDays(10)
                }
            ],
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _tokenService.GetUserIdFromExpiredToken("access").Returns("user-456");
        _userRepository.GetByIdAsync("user-456").Returns(user);
        _tokenService.HashToken("old-refresh").Returns("old-hash");
        _tokenService.GenerateRefreshToken().Returns("new-refresh");
        _tokenService.HashToken("new-refresh").Returns("new-hash");
        _tokenService.GenerateAccessToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>()).Returns("new-access");

        await _service.RefreshTokenAsync("access", "old-refresh");

        await _userRepository.Received(1).ReplaceRefreshTokenAsync(
            "user-456",
            "old-hash",
            Arg.Is<RefreshTokenEntry>(e => e.TokenHash == "new-hash"));
    }
}
