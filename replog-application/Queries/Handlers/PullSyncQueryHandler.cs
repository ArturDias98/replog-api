using Microsoft.Extensions.Logging;
using replog_application.Interfaces;
using replog_application.Mappers;
using replog_shared.Models.Responses;

namespace replog_application.Queries.Handlers;

public class PullSyncQueryHandler(
    IWorkoutRepository workoutRepository,
    ILogger<PullSyncQueryHandler> logger) : IQueryHandler<PullSyncQuery, PullSyncResponse>
{
    public async Task<PullSyncResponse> HandleAsync(PullSyncQuery query)
    {
        var workouts = await workoutRepository.GetByUserIdAsync(query.UserId);

        var response = new PullSyncResponse
        {
            Workouts = workouts.Select(WorkoutMapper.ToDto).ToList(),
            ServerTimestamp = DateTime.UtcNow
        };

        logger.LogInformation("Pull sync returned {Count} workout(s) for user {UserId}", response.Workouts.Count, query.UserId);

        return response;
    }
}
