using FluentValidation;
using replog_application.Interfaces;
using replog_shared.Enums;
using replog_shared.Models.Entities;
using replog_shared.Models.Requests;
using replog_shared.Models.Responses;
using replog_shared.Models.Sync.LogSync;

namespace replog_application.Commands.Handlers.Processors;

public class LogChangeProcessor(
    IValidator<LogSyncModel> addValidator,
    IValidator<UpdateLogSyncModel> updateValidator,
    IValidator<DeleteLogSyncModel> deleteValidator) : IChangeProcessor
{
    public EntityType EntityType => EntityType.Log;

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

                if (!workout.MuscleGroup.TryGetValue(data.MuscleGroupId, out var muscleGroup))
                    return;

                if (!muscleGroup.Exercises.TryGetValue(data.ExerciseId, out var exercise)
                    || exercise.Log.ContainsKey(data.Id))
                {
                    response.AcknowledgedChangeIds.Add(change.Id);
                    return;
                }

                var newLog = new LogEntity
                {
                    Id = data.Id,
                    NumberReps = data.NumberReps,
                    MaxWeight = data.MaxWeight,
                    Date = data.Date
                };

                exercise.Log[data.Id] = newLog;
                workout.UpdatedAt = change.Timestamp;
                dirtyWorkouts.Add(workout.Id);
                break;
            }

            case ChangeAction.Update:
            {
                var data = ChangeDataHelper.DeserializeAndValidate(change.Data, updateValidator);
                if (!workoutCache.TryGetValue(data.WorkoutId, out var workout) || workout.DeletedAt != null)
                    return;

                if (!workout.MuscleGroup.TryGetValue(data.MuscleGroupId, out var muscleGroup))
                    return;

                if (!muscleGroup.Exercises.TryGetValue(data.ExerciseId, out var exercise))
                    return;

                if (!exercise.Log.TryGetValue(data.Id, out var log))
                {
                    response.AcknowledgedChangeIds.Add(change.Id);
                    return;
                }

                log.NumberReps = data.NumberReps;
                log.MaxWeight = data.MaxWeight;

                workout.UpdatedAt = change.Timestamp;
                dirtyWorkouts.Add(workout.Id);
                break;
            }

            case ChangeAction.Delete:
            {
                var data = ChangeDataHelper.DeserializeAndValidate(change.Data, deleteValidator);
                if (!workoutCache.TryGetValue(data.WorkoutId, out var workout) || workout.DeletedAt != null)
                    return;

                if (!workout.MuscleGroup.TryGetValue(data.MuscleGroupId, out var muscleGroup))
                    return;

                if (!muscleGroup.Exercises.TryGetValue(data.ExerciseId, out var exercise))
                    return;

                var removed = exercise.Log.Remove(data.Id);
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
