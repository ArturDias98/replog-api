using System.Text.Json;
using FluentValidation;
using replog_application.Commands;
using replog_application.Interfaces;
using replog_application.Queries;
using replog_shared.Enums;
using replog_shared.Json;
using replog_shared.Models.Entities;
using replog_shared.Models.Requests;
using replog_shared.Models.Responses;
using replog_shared.Models.Sync.Exercise;
using replog_shared.Models.Sync.LogSync;
using replog_shared.Models.Sync.MuscleGroup;
using replog_shared.Models.Sync.Workout;

namespace replog_application.Services;

public class SyncService(
    IWorkoutRepository workoutRepository,
    IValidator<PushSyncRequest> pushValidator) : ISyncService
{
    public async Task<PushSyncResponse> PushAsync(PushSyncCommand command)
    {
        await pushValidator.ValidateAndThrowAsync(command.Request);

        var response = new PushSyncResponse { ServerTimestamp = DateTime.UtcNow };

        var userWorkouts = await workoutRepository.GetByUserIdAsync(command.UserId);
        var workoutCache = userWorkouts.ToDictionary(w => w.Id);
        var dirtyWorkouts = new HashSet<string>();

        var orderedChanges = command.Request.Changes.OrderBy(c => c.Timestamp);

        foreach (var change in orderedChanges)
        {
            ProcessChange(change, command.UserId, workoutCache, dirtyWorkouts, response);
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

    public async Task<PullSyncResponse> PullAsync(PullSyncQuery query)
    {
        var workouts = await workoutRepository.GetByUserIdAsync(query.UserId);

        return new PullSyncResponse
        {
            Workouts = workouts.Select(MapToDto).ToList(),
            ServerTimestamp = DateTime.UtcNow
        };
    }

    private void ProcessChange(
        SyncChangeDto change,
        string userId,
        Dictionary<string, WorkoutEntity> workoutCache,
        HashSet<string> dirtyWorkouts,
        PushSyncResponse response)
    {
        switch (change.EntityType)
        {
            case EntityType.Workout:
                ProcessWorkoutChange(change, userId, workoutCache, dirtyWorkouts, response);
                break;
            case EntityType.MuscleGroup:
                ProcessMuscleGroupChange(change, workoutCache, dirtyWorkouts, response);
                break;
            case EntityType.Exercise:
                ProcessExerciseChange(change, workoutCache, dirtyWorkouts, response);
                break;
            case EntityType.Log:
                ProcessLogChange(change, workoutCache, dirtyWorkouts, response);
                break;
        }
    }

    private void ProcessWorkoutChange(
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
                var data = DeserializeData<AddWorkoutSyncModel>(change.Data);
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
                response.AcknowledgedChangeIds.Add(change.Id);
                break;
            }

            case ChangeAction.Update:
            {
                var data = DeserializeData<UpdateWorkoutSyncModel>(change.Data);
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
                        ServerVersion = MapToDto(workout)
                    });
                    return;
                }

                workout.Title = data.Title;
                workout.Date = data.Date;
                workout.OrderIndex = data.OrderIndex;
                workout.UpdatedAt = change.Timestamp;

                dirtyWorkouts.Add(workout.Id);
                response.AcknowledgedChangeIds.Add(change.Id);
                break;
            }

            case ChangeAction.Delete:
            {
                var data = DeserializeData<DeleteWorkoutSyncModel>(change.Data);
                if (!workoutCache.TryGetValue(data.Id, out var workout) || workout.DeletedAt != null)
                {
                    response.AcknowledgedChangeIds.Add(change.Id);
                    return;
                }

                workout.DeletedAt = change.Timestamp;
                workout.UpdatedAt = change.Timestamp;
                dirtyWorkouts.Add(workout.Id);
                response.AcknowledgedChangeIds.Add(change.Id);
                break;
            }
        }
    }

    private void ProcessMuscleGroupChange(
        SyncChangeDto change,
        Dictionary<string, WorkoutEntity> workoutCache,
        HashSet<string> dirtyWorkouts,
        PushSyncResponse response)
    {
        switch (change.Action)
        {
            case ChangeAction.Create:
            {
                var data = DeserializeData<AddMuscleGroupSyncModel>(change.Data);
                if (!workoutCache.TryGetValue(data.WorkoutId, out var workout) || workout.DeletedAt != null)
                    return;

                if (workout.MuscleGroup.Any(mg => mg.Id == data.Id))
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

                workout.MuscleGroup.Add(newMuscleGroup);
                workout.UpdatedAt = change.Timestamp;
                dirtyWorkouts.Add(workout.Id);
                response.AcknowledgedChangeIds.Add(change.Id);
                break;
            }

            case ChangeAction.Update:
            {
                var data = DeserializeData<UpdateMuscleGroupSyncModel>(change.Data);
                if (!workoutCache.TryGetValue(data.WorkoutId, out var workout) || workout.DeletedAt != null)
                    return;

                var muscleGroup = workout.MuscleGroup.FirstOrDefault(mg => mg.Id == data.Id);
                if (muscleGroup == null)
                {
                    response.AcknowledgedChangeIds.Add(change.Id);
                    return;
                }

                muscleGroup.Title = data.Title;
                muscleGroup.Date = data.Date;
                muscleGroup.OrderIndex = data.OrderIndex;

                workout.UpdatedAt = change.Timestamp;
                dirtyWorkouts.Add(workout.Id);
                response.AcknowledgedChangeIds.Add(change.Id);
                break;
            }

            case ChangeAction.Delete:
            {
                var data = DeserializeData<DeleteMuscleGroupSyncModel>(change.Data);
                if (!workoutCache.TryGetValue(data.WorkoutId, out var workout) || workout.DeletedAt != null)
                    return;

                var removed = workout.MuscleGroup.RemoveAll(mg => mg.Id == data.Id);
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

    private void ProcessExerciseChange(
        SyncChangeDto change,
        Dictionary<string, WorkoutEntity> workoutCache,
        HashSet<string> dirtyWorkouts,
        PushSyncResponse response)
    {
        switch (change.Action)
        {
            case ChangeAction.Create:
            {
                var data = DeserializeData<AddExerciseSyncModel>(change.Data);
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
                var data = DeserializeData<UpdateExerciseSyncModel>(change.Data);
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
                var data = DeserializeData<DeleteExerciseSyncModel>(change.Data);
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

    private void ProcessLogChange(
        SyncChangeDto change,
        Dictionary<string, WorkoutEntity> workoutCache,
        HashSet<string> dirtyWorkouts,
        PushSyncResponse response)
    {
        switch (change.Action)
        {
            case ChangeAction.Create:
            {
                var data = DeserializeData<LogSyncModel>(change.Data);
                if (!workoutCache.TryGetValue(data.WorkoutId, out var workout) || workout.DeletedAt != null)
                    return;

                var muscleGroup = workout.MuscleGroup.FirstOrDefault(mg => mg.Id == data.MuscleGroupId);
                if (muscleGroup == null) return;

                var exercise = muscleGroup.Exercises.FirstOrDefault(e => e.Id == data.ExerciseId);
                if (exercise == null)
                {
                    response.AcknowledgedChangeIds.Add(change.Id);
                    return;
                }

                if (exercise.Log.Any(l => l.Id == data.Id))
                {
                    response.AcknowledgedChangeIds.Add(change.Id);
                    return;
                }

                var newLog = new LogEntity
                {
                    Id = data.Id,
                    NumberReps = data.NumberReps,
                    MaxWeight = data.MaxWeight,
                    Date = data.Date ?? ""
                };

                exercise.Log.Add(newLog);
                workout.UpdatedAt = change.Timestamp;
                dirtyWorkouts.Add(workout.Id);
                response.AcknowledgedChangeIds.Add(change.Id);
                break;
            }

            case ChangeAction.Update:
            {
                var data = DeserializeData<UpdateLogSyncModel>(change.Data);
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
                response.AcknowledgedChangeIds.Add(change.Id);
                break;
            }

            case ChangeAction.Delete:
            {
                var data = DeserializeData<DeleteLogSyncModel>(change.Data);
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

                response.AcknowledgedChangeIds.Add(change.Id);
                break;
            }
        }
    }

    private static T DeserializeData<T>(JsonElement? data)
    {
        if (!data.HasValue)
            throw new ValidationException("Data is required.");

        return data.Value.Deserialize<T>(JsonDefaults.Options)
               ?? throw new ValidationException("Failed to deserialize change data.");
    }

    private static WorkoutDto MapToDto(WorkoutEntity entity)
    {
        return new WorkoutDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Date = entity.Date,
            OrderIndex = entity.OrderIndex,
            MuscleGroup = entity.MuscleGroup.Select(mg => new MuscleGroupDto
            {
                Id = mg.Id,
                WorkoutId = mg.WorkoutId,
                Title = mg.Title,
                Date = mg.Date,
                OrderIndex = mg.OrderIndex,
                Exercises = mg.Exercises.Select(e => new ExerciseDto
                {
                    Id = e.Id,
                    MuscleGroupId = e.MuscleGroupId,
                    Title = e.Title,
                    OrderIndex = e.OrderIndex,
                    Log = e.Log.Select(l => new LogDto
                    {
                        Id = l.Id,
                        NumberReps = l.NumberReps,
                        MaxWeight = l.MaxWeight,
                        Date = l.Date
                    }).ToList()
                }).ToList()
            }).ToList()
        };
    }
}
