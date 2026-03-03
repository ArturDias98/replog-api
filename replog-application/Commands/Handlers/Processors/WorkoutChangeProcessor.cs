using FluentValidation;
using replog_application.Interfaces;
using replog_application.Mappers;
using replog_shared.Enums;
using replog_domain.Entities;
using replog_shared.Models.Requests;
using replog_shared.Models.Responses;
using replog_shared.Models.Sync.Workout;

namespace replog_application.Commands.Handlers.Processors;

public class WorkoutChangeProcessor(
    IValidator<AddWorkoutSyncModel> addValidator,
    IValidator<UpdateWorkoutSyncModel> updateValidator,
    IValidator<DeleteWorkoutSyncModel> deleteValidator) : IChangeProcessor
{
    public EntityType EntityType => EntityType.Workout;

    public void Process(
        SyncChangeDto change,
        string userId,
        Dictionary<string, WorkoutEntity> workoutCache,
        HashSet<string> dirtyWorkouts,
        PushSyncResponse response)
    {
        switch (change.Action)
        {
            case ChangeAction.Create:
            {
                var data = ChangeDataHelper.DeserializeAndValidate(change.Data, addValidator);
                if (workoutCache.ContainsKey(data.Id))
                {
                    response.AcknowledgedChangeIds.Add(change.Id);
                    return;
                }

                var newWorkout = new WorkoutEntity
                {
                    Id = data.Id,
                    UserId = userId,
                    Title = data.Title,
                    Date = data.Date,
                    OrderIndex = data.OrderIndex,
                    CreatedAt = change.Timestamp,
                    UpdatedAt = change.Timestamp
                };

                workoutCache[newWorkout.Id] = newWorkout;
                dirtyWorkouts.Add(newWorkout.Id);
                break;
            }

            case ChangeAction.Update:
            {
                var data = ChangeDataHelper.DeserializeAndValidate(change.Data, updateValidator);
                if (!workoutCache.TryGetValue(data.Id, out var workout) || workout.DeletedAt != null)
                {
                    response.AcknowledgedChangeIds.Add(change.Id);
                    return;
                }

                if (workout.UpdatedAt > change.Timestamp)
                {
                    response.Conflicts.Add(new ConflictDto
                    {
                        ChangeId = change.Id,
                        ServerVersion = WorkoutMapper.ToDto(workout)
                    });
                    return;
                }

                workout.Title = data.Title;
                workout.Date = data.Date;
                workout.OrderIndex = data.OrderIndex;
                workout.UpdatedAt = change.Timestamp;

                dirtyWorkouts.Add(workout.Id);
                break;
            }

            case ChangeAction.Delete:
            {
                var data = ChangeDataHelper.DeserializeAndValidate(change.Data, deleteValidator);
                if (!workoutCache.TryGetValue(data.Id, out var workout) || workout.DeletedAt != null)
                {
                    response.AcknowledgedChangeIds.Add(change.Id);
                    return;
                }

                workout.DeletedAt = change.Timestamp;
                workout.UpdatedAt = change.Timestamp;
                dirtyWorkouts.Add(workout.Id);
                break;
            }
            default:
                throw new Exception("Invalid operation");
        }

        response.AcknowledgedChangeIds.Add(change.Id);
    }
}
