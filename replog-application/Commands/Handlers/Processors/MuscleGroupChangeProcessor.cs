using FluentValidation;
using replog_application.Interfaces;
using replog_application.Interfaces.SyncOperations;
using replog_shared.Enums;
using replog_domain.Entities;
using replog_shared.Helpers;
using replog_shared.Models.Requests;
using replog_shared.Models.Responses;
using replog_shared.Models.Sync.MuscleGroup;

namespace replog_application.Commands.Handlers.Processors;

public class MuscleGroupChangeProcessor(
    IMuscleGroupSyncRepository muscleGroupSync,
    IValidator<AddMuscleGroupSyncModel> addValidator,
    IValidator<UpdateMuscleGroupSyncModel> updateValidator,
    IValidator<DeleteMuscleGroupSyncModel> deleteValidator) : IChangeProcessor
{
    public EntityType EntityType => EntityType.MuscleGroup;

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

                    var newMuscleGroup = new MuscleGroupEntity
                    {
                        Id = data.Id,
                        WorkoutId = data.WorkoutId,
                        Title = data.Title,
                        Date = data.Date,
                        OrderIndex = data.OrderIndex
                    };

                    await muscleGroupSync.AddMuscleGroupAsync(
                        userId, newMuscleGroup, change.Timestamp, cancellationToken);
                    break;
                }

            case ChangeAction.Update:
                {
                    var data = ChangeDataHelper.DeserializeAndValidate(change.Data, updateValidator);
                    await muscleGroupSync.UpdateMuscleGroupAsync(
                        userId, data.WorkoutId, data.Id, data.Title, data.Date, data.OrderIndex, change.Timestamp, cancellationToken);
                    break;
                }

            case ChangeAction.Delete:
                {
                    var data = ChangeDataHelper.DeserializeAndValidate(change.Data, deleteValidator);
                    await muscleGroupSync.RemoveMuscleGroupAsync(
                        userId, data.WorkoutId, data.Id, change.Timestamp, cancellationToken);
                    break;
                }
            default:
                throw new Exception("Invalid operation");
        }

        response.AcknowledgedChangeIds.Add(change.Id);
    }
}
