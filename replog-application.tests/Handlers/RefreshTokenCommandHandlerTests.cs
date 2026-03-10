using NSubstitute;
using replog_application.Commands;
using replog_application.Commands.Handlers;
using replog_application.Interfaces;
using replog_domain.Entities;

namespace replog_application.tests.Handlers;

public class RefreshTokenCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _handler = new RefreshTokenCommandHandler(_userRepository, _tokenService);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNewTokens_WhenRefreshTokenIsValid()
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

        var command = new RefreshTokenCommand
        {
            AccessToken = "expired-access",
            RefreshToken = "valid-refresh"
        };
        var result = await _handler.HandleAsync(command);

        Assert.Equal("new-access-token", result.AccessToken);
        Assert.Equal("new-refresh-token", result.RefreshToken);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowUnauthorized_WhenAccessTokenIsInvalid()
    {
        _tokenService.GetUserIdFromExpiredToken("bad-token").Returns((string?)null);

        var command = new RefreshTokenCommand
        {
            AccessToken = "bad-token",
            RefreshToken = "some-refresh"
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowUnauthorized_WhenUserNotFound()
    {
        _tokenService.GetUserIdFromExpiredToken("token").Returns("user-123");
        _userRepository.GetByIdAsync("user-123").Returns((UserEntity?)null);

        var command = new RefreshTokenCommand
        {
            AccessToken = "token",
            RefreshToken = "refresh"
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowUnauthorized_WhenRefreshTokenIsExpired()
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

        var command = new RefreshTokenCommand
        {
            AccessToken = "token",
            RefreshToken = "refresh"
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowUnauthorized_WhenRefreshTokenHashDoesNotMatch()
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

        var command = new RefreshTokenCommand
        {
            AccessToken = "token",
            RefreshToken = "wrong-refresh"
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_ShouldCallReplaceRefreshToken_WhenTokenIsValid()
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

        var command = new RefreshTokenCommand
        {
            AccessToken = "access",
            RefreshToken = "old-refresh"
        };
        await _handler.HandleAsync(command);

        await _userRepository.Received(1).ReplaceRefreshTokenAsync(
            "user-456",
            "old-hash",
            Arg.Is<RefreshTokenEntry>(e => e.TokenHash == "new-hash"));
    }
}
