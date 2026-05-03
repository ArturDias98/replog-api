using FluentValidation;
using replog_application.Interfaces;
using replog_application.Interfaces.SyncOperations;
using replog_application.Mappers;
using replog_shared.Enums;
using replog_domain.Entities;
using replog_shared.Helpers;
using replog_shared.Models.Requests;
using replog_shared.Models.Responses;
using replog_shared.Models.Sync.Workout;

namespace replog_application.Commands.Handlers.Processors;

public class WorkoutChangeProcessor(
    IWorkoutSyncRepository workoutSync,
    IValidator<AddWorkoutSyncModel> addValidator,
    IValidator<UpdateWorkoutSyncModel> updateValidator,
    IValidator<DeleteWorkoutSyncModel> deleteValidator) : IChangeProcessor
{
    public EntityType EntityType => EntityType.Workout;

    public async Task ProcessAsync(
        SyncChangeDto change,
        string userId,
        PushSyncResponse response,
        CancellationToken cancellationToken = default)
    {
        switch (change.Action)
        {
            case ChangeAction.Create:
                {
                    var data = ChangeDataHelper.DeserializeAndValidate(change.Data, addValidator);

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

                    await workoutSync.CreateWorkoutAsync(newWorkout, cancellationToken);
                    break;
                }

            case ChangeAction.Update:
                {
                    var data = ChangeDataHelper.DeserializeAndValidate(change.Data, updateValidator);

                    var conflictWorkout = await workoutSync.UpdateWorkoutAsync(
                        userId, data.Id, data.Title, data.Date, data.OrderIndex, change.Timestamp, cancellationToken);

                    if (conflictWorkout is not null)
                    {
                        response.Conflicts.Add(new ConflictDto
                        {
                            ChangeId = change.Id,
                            ServerVersion = WorkoutMapper.ToDto(conflictWorkout)
                        });
                        return;
                    }

                    break;
                }

            case ChangeAction.Delete:
                {
                    var data = ChangeDataHelper.DeserializeAndValidate(change.Data, deleteValidator);
                    await workoutSync.SoftDeleteWorkoutAsync(userId, data.Id, change.Timestamp, cancellationToken);
                    break;
                }
            default:
                throw new Exception("Invalid operation");
        }

        response.AcknowledgedChangeIds.Add(change.Id);
    }
}
