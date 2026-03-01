using FluentValidation;
using replog_application.Interfaces;
using replog_shared.Enums;
using replog_shared.Models.Requests;
using replog_shared.Models.Responses;

namespace replog_application.Commands.Handlers;

public class PushSyncCommandHandler(
    IWorkoutRepository workoutRepository,
    IValidator<PushSyncRequest> pushValidator,
    IEnumerable<IChangeProcessor> changeProcessors) : ICommandHandler<PushSyncCommand, PushSyncResponse>
{
    private readonly Dictionary<EntityType, IChangeProcessor> _processors =
        changeProcessors.ToDictionary(p => p.EntityType);

    public async Task<PushSyncResponse> HandleAsync(PushSyncCommand command)
    {
        await pushValidator.ValidateAndThrowAsync(command.Request);

        var response = new PushSyncResponse { ServerTimestamp = DateTime.UtcNow };

        var userWorkouts = await workoutRepository.GetByUserIdAsync(command.UserId);
        var workoutCache = userWorkouts.ToDictionary(w => w.Id);
        var dirtyWorkouts = new HashSet<string>();

        var orderedChanges = command.Request.Changes.OrderBy(c => c.Timestamp);

        foreach (var change in orderedChanges)
        {
            if (_processors.TryGetValue(change.EntityType, out var processor))
            {
                processor.Process(change, command.UserId, workoutCache, dirtyWorkouts, response);
            }
        }

        foreach (var workoutId in dirtyWorkouts)
        {
            if (workoutCache.TryGetValue(workoutId, out var workout))
            {
                await workoutRepository.PutAsync(workout);
            }
        }

        return response;
    }
}
