using replog_domain.Entities;

namespace replog_application.Interfaces;

public interface IUserRepository
{
    Task<UserEntity?> GetByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task UpsertAsync(UserEntity user, CancellationToken cancellationToken = default);
    Task AddRefreshTokenAsync(string userId, RefreshTokenEntry entry, CancellationToken cancellationToken = default);
    Task ReplaceRefreshTokenAsync(string userId, string oldTokenHash, RefreshTokenEntry newEntry, CancellationToken cancellationToken = default);
}
