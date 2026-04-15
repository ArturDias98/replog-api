using replog_domain.Entities;

namespace replog_application.Interfaces.SyncOperations;

public interface IMuscleGroupSyncRepository
{
    Task<bool> AddMuscleGroupAsync(string userId, MuscleGroupEntity muscleGroup, DateTime timestamp, CancellationToken cancellationToken = default);
    Task<bool> UpdateMuscleGroupAsync(string userId, string workoutId, string mgId, string title, string date, int orderIndex, DateTime timestamp, CancellationToken cancellationToken = default);
    Task<bool> RemoveMuscleGroupAsync(string userId, string workoutId, string mgId, DateTime timestamp, CancellationToken cancellationToken = default);
}
