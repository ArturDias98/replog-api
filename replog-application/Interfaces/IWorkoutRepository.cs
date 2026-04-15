using replog_domain.Entities;

namespace replog_application.Interfaces;

public interface IWorkoutRepository
{
    Task<WorkoutEntity?> GetByIdAsync(string workoutId, CancellationToken cancellationToken = default);
    Task<List<WorkoutEntity>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task PutAsync(WorkoutEntity workout, CancellationToken cancellationToken = default);
}
