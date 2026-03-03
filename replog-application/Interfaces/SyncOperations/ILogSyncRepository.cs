using replog_domain.Entities;

namespace replog_application.Interfaces.SyncOperations;

public interface ILogSyncRepository
{
    Task<bool> AddLogAsync(string workoutId, string userId, string mgId, string exId, LogEntity log, DateTime timestamp);
    Task<bool> UpdateLogAsync(string workoutId, string mgId, string exId, string logId, int numberReps, double maxWeight, DateTime timestamp);
    Task<bool> RemoveLogAsync(string workoutId, string mgId, string exId, string logId, DateTime timestamp);
}
