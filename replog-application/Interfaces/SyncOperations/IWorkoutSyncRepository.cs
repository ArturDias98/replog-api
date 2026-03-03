using replog_domain.Entities;

namespace replog_application.Interfaces.SyncOperations;

public interface IWorkoutSyncRepository
{
    Task<bool> CreateWorkoutAsync(WorkoutEntity workout);
    Task<WorkoutEntity?> UpdateWorkoutAsync(string workoutId, string title, string date, int orderIndex, DateTime timestamp);
    Task<bool> SoftDeleteWorkoutAsync(string workoutId, DateTime timestamp);
}
