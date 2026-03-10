using replog_domain.Entities;

namespace replog_application.Interfaces;

public interface IUserRepository
{
    Task<UserEntity?> GetByIdAsync(string userId);
    Task UpsertAsync(UserEntity user);
    Task AddRefreshTokenAsync(string userId, RefreshTokenEntry entry);
    Task ReplaceRefreshTokenAsync(string userId, string oldTokenHash, RefreshTokenEntry newEntry);
}
