using FluentValidation;
using replog_application.Interfaces;
using replog_shared.Enums;
using replog_shared.Models.Entities;
using replog_shared.Models.Requests;
using replog_shared.Models.Responses;
using replog_shared.Models.Sync.MuscleGroup;

namespace replog_application.Commands.Handlers.Processors;

public class MuscleGroupChangeProcessor(
    IValidator<AddMuscleGroupSyncModel> addValidator,
    IValidator<UpdateMuscleGroupSyncModel> updateValidator,
    IValidator<DeleteMuscleGroupSyncModel> deleteValidator) : IChangeProcessor
{
    public EntityType EntityType => EntityType.MuscleGroup;

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
                if (!workoutCache.TryGetValue(data.WorkoutId, out var workout) || workout.DeletedAt != null)
                    return;

                if (workout.MuscleGroup.ContainsKey(data.Id))
                {
                    response.AcknowledgedChangeIds.Add(change.Id);
                    return;
                }

                var newMuscleGroup = new MuscleGroupEntity
                {
                    Id = data.Id,
                    WorkoutId = data.WorkoutId,
                    Title = data.Title,
                    Date = data.Date,
                    OrderIndex = data.OrderIndex
                };

                workout.MuscleGroup[data.Id] = newMuscleGroup;
                workout.UpdatedAt = change.Timestamp;
                dirtyWorkouts.Add(workout.Id);
                break;
            }

            case ChangeAction.Update:
            {
                var data = ChangeDataHelper.DeserializeAndValidate(change.Data, updateValidator);
                if (!workoutCache.TryGetValue(data.WorkoutId, out var workout) || workout.DeletedAt != null)
                    return;

                if (!workout.MuscleGroup.TryGetValue(data.Id, out var muscleGroup))
                {
                    response.AcknowledgedChangeIds.Add(change.Id);
                    return;
                }

                muscleGroup.Title = data.Title;
                muscleGroup.Date = data.Date;
                muscleGroup.OrderIndex = data.OrderIndex;

                workout.UpdatedAt = change.Timestamp;
                dirtyWorkouts.Add(workout.Id);
                break;
            }

            case ChangeAction.Delete:
            {
                var data = ChangeDataHelper.DeserializeAndValidate(change.Data, deleteValidator);
                if (!workoutCache.TryGetValue(data.WorkoutId, out var workout) || workout.DeletedAt != null)
                    return;

                var removed = workout.MuscleGroup.Remove(data.Id);
                if (removed)
                {
                    workout.UpdatedAt = change.Timestamp;
                    dirtyWorkouts.Add(workout.Id);
                }

                break;
            }
            default:
                throw new Exception("Invalid operation");
        }

        response.AcknowledgedChangeIds.Add(change.Id);
    }
}
