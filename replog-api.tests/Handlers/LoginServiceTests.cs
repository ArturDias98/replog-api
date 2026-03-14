using NSubstitute;
using replog_api.Auth;
using replog_application.Interfaces;
using replog_domain.Entities;

namespace replog_api.tests.Handlers;

public class LoginServiceTests
{
    private readonly IGoogleTokenValidator _googleValidator = Substitute.For<IGoogleTokenValidator>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly AuthService _service;

    public LoginServiceTests()
    {
        _service = new AuthService(_googleValidator, _userRepository, _tokenService);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnAuthResponse_WhenGoogleTokenIsValid()
    {
        var googleUser = new GoogleUserInfo
        {
            Subject = "google-sub-123",
            Email = "user@example.com",
            Name = "Test User",
            Picture = "https://example.com/avatar.png"
        };
        _googleValidator.ValidateAsync("valid-token").Returns(googleUser);
        _tokenService.GenerateAccessToken("google-sub-123", "user@example.com", "Test User", "https://example.com/avatar.png").Returns("access-token");
        _tokenService.GenerateRefreshToken().Returns("refresh-token");
        _tokenService.HashToken("refresh-token").Returns("hashed-refresh-token");

        var result = await _service.LoginAsync("valid-token");

        Assert.NotNull(result);
        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_ShouldCreateNewUser_WhenUserDoesNotExist()
    {
        var googleUser = new GoogleUserInfo
        {
            Subject = "new-user-sub",
            Email = "new@example.com",
            Name = "New User"
        };
        _googleValidator.ValidateAsync("valid-token").Returns(googleUser);
        _userRepository.GetByIdAsync("new-user-sub").Returns((UserEntity?)null);
        _tokenService.GenerateAccessToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>()).Returns("access-token");
        _tokenService.GenerateRefreshToken().Returns("refresh-token");
        _tokenService.HashToken("refresh-token").Returns("hashed");

        await _service.LoginAsync("valid-token");

        await _userRepository.Received(1).UpsertAsync(Arg.Is<UserEntity>(u =>
            u.Id == "new-user-sub" &&
            u.Email == "new@example.com" &&
            u.DisplayName == "New User" &&
            u.RefreshTokens.Count == 1 &&
            u.RefreshTokens[0].TokenHash == "hashed"));
    }

    [Fact]
    public async Task LoginAsync_ShouldUpdateExistingUser_WhenUserExists()
    {
        var existingUser = new UserEntity
        {
            Id = "existing-sub",
            Email = "old@example.com",
            DisplayName = "Old Name",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var googleUser = new GoogleUserInfo
        {
            Subject = "existing-sub",
            Email = "new@example.com",
            Name = "New Name",
            Picture = "https://example.com/new-avatar.png"
        };
        _googleValidator.ValidateAsync("valid-token").Returns(googleUser);
        _userRepository.GetByIdAsync("existing-sub").Returns(existingUser);
        _tokenService.GenerateAccessToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>()).Returns("access-token");
        _tokenService.GenerateRefreshToken().Returns("refresh-token");
        _tokenService.HashToken("refresh-token").Returns("hashed");

        await _service.LoginAsync("valid-token");

        await _userRepository.Received(1).UpsertAsync(Arg.Is<UserEntity>(u =>
            u.Email == "new@example.com" &&
            u.DisplayName == "New Name" &&
            u.AvatarUrl == "https://example.com/new-avatar.png"));
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowUnauthorized_WhenGoogleTokenIsInvalid()
    {
        _googleValidator.ValidateAsync("invalid-token").Returns((GoogleUserInfo?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.LoginAsync("invalid-token"));
    }

    [Fact]
    public async Task LoginAsync_ShouldAddTokenToList_WhenUserHasExistingTokens()
    {
        var existingUser = new UserEntity
        {
            Id = "multi-device-sub",
            Email = "user@example.com",
            DisplayName = "User",
            RefreshTokens =
            [
                new RefreshTokenEntry
                {
                    TokenHash = "device-a-hash",
                    ExpiresAt = DateTime.UtcNow.AddDays(20)
                }
            ],
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var googleUser = new GoogleUserInfo
        {
            Subject = "multi-device-sub",
            Email = "user@example.com",
            Name = "User"
        };
        _googleValidator.ValidateAsync("token").Returns(googleUser);
        _userRepository.GetByIdAsync("multi-device-sub").Returns(existingUser);
        _tokenService.GenerateRefreshToken().Returns("new-refresh");
        _tokenService.HashToken("new-refresh").Returns("new-hash");
        _tokenService.GenerateAccessToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>()).Returns("access");

        await _service.LoginAsync("token");

        await _userRepository.Received(1).UpsertAsync(Arg.Is<UserEntity>(u =>
            u.RefreshTokens.Count == 2 &&
            u.RefreshTokens[0].TokenHash == "device-a-hash" &&
            u.RefreshTokens[1].TokenHash == "new-hash"));
    }

    [Fact]
    public async Task LoginAsync_ShouldRemoveExpiredTokens_WhenLoginOccurs()
    {
        var existingUser = new UserEntity
        {
            Id = "cleanup-sub",
            Email = "user@example.com",
            DisplayName = "User",
            RefreshTokens =
            [
                new RefreshTokenEntry
                {
                    TokenHash = "expired-hash",
                    ExpiresAt = DateTime.UtcNow.AddDays(-5)
                },
                new RefreshTokenEntry
                {
                    TokenHash = "valid-hash",
                    ExpiresAt = DateTime.UtcNow.AddDays(20)
                }
            ],
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var googleUser = new GoogleUserInfo
        {
            Subject = "cleanup-sub",
            Email = "user@example.com",
            Name = "User"
        };
        _googleValidator.ValidateAsync("token").Returns(googleUser);
        _userRepository.GetByIdAsync("cleanup-sub").Returns(existingUser);
        _tokenService.GenerateRefreshToken().Returns("new-refresh");
        _tokenService.HashToken("new-refresh").Returns("new-hash");
        _tokenService.GenerateAccessToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>()).Returns("access");

        await _service.LoginAsync("token");

        await _userRepository.Received(1).UpsertAsync(Arg.Is<UserEntity>(u =>
            u.RefreshTokens.Count == 2 &&
            u.RefreshTokens[0].TokenHash == "valid-hash" &&
            u.RefreshTokens[1].TokenHash == "new-hash"));
    }
}
