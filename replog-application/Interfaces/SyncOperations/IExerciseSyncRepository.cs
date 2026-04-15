using replog_domain.Entities;

namespace replog_application.Interfaces.SyncOperations;

public interface IExerciseSyncRepository
{
    Task<bool> AddExerciseAsync(string workoutId, string userId, ExerciseEntity exercise, DateTime timestamp, CancellationToken cancellationToken = default);
    Task<bool> UpdateExerciseAsync(string userId, string workoutId, string mgId, string exId, string title, int orderIndex, DateTime timestamp, CancellationToken cancellationToken = default);
    Task<bool> RemoveExerciseAsync(string userId, string workoutId, string mgId, string exId, DateTime timestamp, CancellationToken cancellationToken = default);
}
