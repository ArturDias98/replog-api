using replog_domain.Entities;

namespace replog_application.Interfaces.SyncOperations;

public interface IExerciseSyncRepository
{
    Task<bool> AddExerciseAsync(string workoutId, string userId, ExerciseEntity exercise, DateTime timestamp);
    Task<bool> UpdateExerciseAsync(string workoutId, string mgId, string exId, string title, int orderIndex, DateTime timestamp);
    Task<bool> RemoveExerciseAsync(string workoutId, string mgId, string exId, DateTime timestamp);
}
