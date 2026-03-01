using replog_application.Interfaces;
using replog_application.Mappers;
using replog_shared.Models.Responses;

namespace replog_application.Queries.Handlers;

public class PullSyncQueryHandler(
    IWorkoutRepository workoutRepository) : IQueryHandler<PullSyncQuery, PullSyncResponse>
{
    public async Task<PullSyncResponse> HandleAsync(PullSyncQuery query)
    {
        var workouts = await workoutRepository.GetByUserIdAsync(query.UserId);

        return new PullSyncResponse
        {
            Workouts = workouts.Select(WorkoutMapper.ToDto).ToList(),
            ServerTimestamp = DateTime.UtcNow
        };
    }
}
