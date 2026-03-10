using Microsoft.Extensions.Options;
using replog_domain.Entities;
using replog_infrastructure.Repositories;
using replog_infrastructure.Settings;
using replog_tests_shared.Fixtures;

namespace replog_infrastructure.tests.Repositories;

[Collection("DynamoDB")]
public class UserRepositoryTests
{
    private readonly UserRepository _repository;

    public UserRepositoryTests(DynamoDbFixture fixture)
    {
        var settings = Options.Create(new DynamoDbSettings());
        _repository = new UserRepository(fixture.Client, settings);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        var result = await _repository.GetByIdAsync("nonexistent-user");
        Assert.Null(result);
    }

    [Fact]
    public async Task UpsertAsync_ShouldCreateUser_WhenUserIsNew()
    {
        var user = new UserEntity
        {
            Id = "upsert-create-test",
            Email = "test@example.com",
            DisplayName = "Test User",
            AvatarUrl = "https://example.com/avatar.png",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.UpsertAsync(user);

        var result = await _repository.GetByIdAsync("upsert-create-test");
        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("Test User", result.DisplayName);
        Assert.Equal("https://example.com/avatar.png", result.AvatarUrl);
    }

    [Fact]
    public async Task UpsertAsync_ShouldUpdateUser_WhenUserExists()
    {
        var user = new UserEntity
        {
            Id = "upsert-update-test",
            Email = "old@example.com",
            DisplayName = "Old Name",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.UpsertAsync(user);

        user.Email = "new@example.com";
        user.DisplayName = "New Name";
        user.UpdatedAt = DateTime.UtcNow;
        await _repository.UpsertAsync(user);

        var result = await _repository.GetByIdAsync("upsert-update-test");
        Assert.NotNull(result);
        Assert.Equal("new@example.com", result.Email);
        Assert.Equal("New Name", result.DisplayName);
    }

    [Fact]
    public async Task UpsertAsync_ShouldPersistRefreshTokensList()
    {
        var user = new UserEntity
        {
            Id = "tokens-list-test",
            Email = "test@example.com",
            DisplayName = "Test User",
            RefreshTokens =
            [
                new RefreshTokenEntry
                {
                    TokenHash = "hash-1",
                    ExpiresAt = DateTime.UtcNow.AddDays(30)
                },
                new RefreshTokenEntry
                {
                    TokenHash = "hash-2",
                    ExpiresAt = DateTime.UtcNow.AddDays(30)
                }
            ],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.UpsertAsync(user);

        var result = await _repository.GetByIdAsync("tokens-list-test");
        Assert.NotNull(result);
        Assert.Equal(2, result.RefreshTokens.Count);
        Assert.Equal("hash-1", result.RefreshTokens[0].TokenHash);
        Assert.Equal("hash-2", result.RefreshTokens[1].TokenHash);
    }

    [Fact]
    public async Task AddRefreshTokenAsync_ShouldAppendToList()
    {
        var user = new UserEntity
        {
            Id = "add-token-test",
            Email = "test@example.com",
            DisplayName = "Test User",
            RefreshTokens =
            [
                new RefreshTokenEntry
                {
                    TokenHash = "existing-hash",
                    ExpiresAt = DateTime.UtcNow.AddDays(20)
                }
            ],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.UpsertAsync(user);

        var newEntry = new RefreshTokenEntry
        {
            TokenHash = "new-hash",
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
        await _repository.AddRefreshTokenAsync("add-token-test", newEntry);

        var result = await _repository.GetByIdAsync("add-token-test");
        Assert.NotNull(result);
        Assert.Equal(2, result.RefreshTokens.Count);
        Assert.Equal("existing-hash", result.RefreshTokens[0].TokenHash);
        Assert.Equal("new-hash", result.RefreshTokens[1].TokenHash);
    }

    [Fact]
    public async Task ReplaceRefreshTokenAsync_ShouldReplaceMatchingToken()
    {
        var user = new UserEntity
        {
            Id = "replace-token-test",
            Email = "test@example.com",
            DisplayName = "Test User",
            RefreshTokens =
            [
                new RefreshTokenEntry
                {
                    TokenHash = "device-a-hash",
                    ExpiresAt = DateTime.UtcNow.AddDays(20)
                },
                new RefreshTokenEntry
                {
                    TokenHash = "device-b-hash",
                    ExpiresAt = DateTime.UtcNow.AddDays(25)
                }
            ],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.UpsertAsync(user);

        var newEntry = new RefreshTokenEntry
        {
            TokenHash = "device-a-new-hash",
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
        await _repository.ReplaceRefreshTokenAsync("replace-token-test", "device-a-hash", newEntry);

        var result = await _repository.GetByIdAsync("replace-token-test");
        Assert.NotNull(result);
        Assert.Equal(2, result.RefreshTokens.Count);
        Assert.Contains(result.RefreshTokens, rt => rt.TokenHash == "device-a-new-hash");
        Assert.Contains(result.RefreshTokens, rt => rt.TokenHash == "device-b-hash");
    }

    [Fact]
    public async Task UpsertAsync_ShouldHandleNullAvatarUrl()
    {
        var user = new UserEntity
        {
            Id = "null-avatar-test",
            Email = "test@example.com",
            DisplayName = "Test User",
            AvatarUrl = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.UpsertAsync(user);

        var result = await _repository.GetByIdAsync("null-avatar-test");
        Assert.NotNull(result);
        Assert.Null(result.AvatarUrl);
    }
}
