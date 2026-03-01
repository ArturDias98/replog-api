using FluentValidation;
using replog_application.Interfaces;
using replog_shared.Enums;
using replog_shared.Models.Entities;
using replog_shared.Models.Requests;
using replog_shared.Models.Responses;
using replog_shared.Models.Sync.Exercise;

namespace replog_application.Commands.Handlers.Processors;

public class ExerciseChangeProcessor(
    IValidator<AddExerciseSyncModel> addValidator,
    IValidator<UpdateExerciseSyncModel> updateValidator,
    IValidator<DeleteExerciseSyncModel> deleteValidator) : IChangeProcessor
{
    public EntityType EntityType => EntityType.Exercise;

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
                if (muscleGroup == null)
                {
                    response.AcknowledgedChangeIds.Add(change.Id);
                    return;
                }

                if (muscleGroup.Exercises.Any(e => e.Id == data.Id))
                {
                    response.AcknowledgedChangeIds.Add(change.Id);
                    return;
                }

                var newExercise = new ExerciseEntity
                {
                    Id = data.Id,
                    MuscleGroupId = data.MuscleGroupId,
                    Title = data.Title,
                    OrderIndex = data.OrderIndex
                };

                muscleGroup.Exercises.Add(newExercise);
                workout.UpdatedAt = change.Timestamp;
                dirtyWorkouts.Add(workout.Id);
                response.AcknowledgedChangeIds.Add(change.Id);
                break;
            }

            case ChangeAction.Update:
            {
                var data = ChangeDataHelper.DeserializeAndValidate(change.Data, updateValidator);
                if (!workoutCache.TryGetValue(data.WorkoutId, out var workout) || workout.DeletedAt != null)
                    return;

                var muscleGroup = workout.MuscleGroup.FirstOrDefault(mg => mg.Id == data.MuscleGroupId);
                if (muscleGroup == null)
                {
                    response.AcknowledgedChangeIds.Add(change.Id);
                    return;
                }

                var exercise = muscleGroup.Exercises.FirstOrDefault(e => e.Id == data.Id);
                if (exercise == null)
                {
                    response.AcknowledgedChangeIds.Add(change.Id);
                    return;
                }

                exercise.Title = data.Title;
                exercise.OrderIndex = data.OrderIndex;

                workout.UpdatedAt = change.Timestamp;
                dirtyWorkouts.Add(workout.Id);
                response.AcknowledgedChangeIds.Add(change.Id);
                break;
            }

            case ChangeAction.Delete:
            {
                var data = ChangeDataHelper.DeserializeAndValidate(change.Data, deleteValidator);
                if (!workoutCache.TryGetValue(data.WorkoutId, out var workout) || workout.DeletedAt != null)
                    return;

                var muscleGroup = workout.MuscleGroup.FirstOrDefault(mg => mg.Id == data.MuscleGroupId);
                if (muscleGroup == null)
                    return;

                var removed = muscleGroup.Exercises.RemoveAll(e => e.Id == data.Id);
                if (removed > 0)
                {
                    workout.UpdatedAt = change.Timestamp;
                    dirtyWorkouts.Add(workout.Id);
                }

                response.AcknowledgedChangeIds.Add(change.Id);
                break;
            }
        }
    }
}
