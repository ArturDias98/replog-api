using replog_domain.Entities;

namespace replog_application.Interfaces;

public interface IWorkoutRepository
{
    Task<WorkoutEntity?> GetByIdAsync(string workoutId);
    Task<List<WorkoutEntity>> GetByUserIdAsync(string userId);
    Task PutAsync(WorkoutEntity workout);
}
