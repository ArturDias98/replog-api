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

                var muscleGroup = workout.MuscleGroup.FirstOrDefault(mg => mg.Id == data.MuscleGroupId);
                if (muscleGroup == null) return;

                var exercise = muscleGroup.Exercises.FirstOrDefault(e => e.Id == data.ExerciseId);
                if (exercise == null || exercise.Log.Any(l => l.Id == data.Id))
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

                exercise.Log.Add(newLog);
                workout.UpdatedAt = change.Timestamp;
                dirtyWorkouts.Add(workout.Id);
                break;
            }

            case ChangeAction.Update:
            {
                var data = ChangeDataHelper.DeserializeAndValidate(change.Data, updateValidator);
                if (!workoutCache.TryGetValue(data.WorkoutId, out var workout) || workout.DeletedAt != null)
                    return;

                var muscleGroup = workout.MuscleGroup.FirstOrDefault(mg => mg.Id == data.MuscleGroupId);
                if (muscleGroup == null) return;

                var exercise = muscleGroup.Exercises.FirstOrDefault(e => e.Id == data.ExerciseId);
                if (exercise == null) return;

                var log = exercise.Log.FirstOrDefault(l => l.Id == data.Id);
                if (log == null)
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

                var muscleGroup = workout.MuscleGroup.FirstOrDefault(mg => mg.Id == data.MuscleGroupId);
                if (muscleGroup == null) return;

                var exercise = muscleGroup.Exercises.FirstOrDefault(e => e.Id == data.ExerciseId);
                if (exercise == null) return;

                var removed = exercise.Log.RemoveAll(l => l.Id == data.Id);
                if (removed > 0)
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
