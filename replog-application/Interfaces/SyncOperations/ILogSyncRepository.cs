using replog_domain.Entities;

namespace replog_application.Interfaces.SyncOperations;

public interface ILogSyncRepository
{
    Task<bool> AddLogAsync(string workoutId, string userId, string mgId, string exId, LogEntity log, DateTime timestamp, CancellationToken cancellationToken = default);
    Task<bool> UpdateLogAsync(string userId, string workoutId, string mgId, string exId, string logId, int numberReps, double maxWeight, DateTime timestamp, CancellationToken cancellationToken = default);
    Task<bool> RemoveLogAsync(string userId, string workoutId, string mgId, string exId, string logId, DateTime timestamp, CancellationToken cancellationToken = default);
}
