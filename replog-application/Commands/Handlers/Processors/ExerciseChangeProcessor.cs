using FluentValidation;
using replog_application.Interfaces;
using replog_application.Interfaces.SyncOperations;
using replog_shared.Enums;
using replog_domain.Entities;
using replog_shared.Helpers;
using replog_shared.Models.Requests;
using replog_shared.Models.Responses;
using replog_shared.Models.Sync.Exercise;

namespace replog_application.Commands.Handlers.Processors;

public class ExerciseChangeProcessor(
    IExerciseSyncRepository exerciseSync,
    IValidator<AddExerciseSyncModel> addValidator,
    IValidator<UpdateExerciseSyncModel> updateValidator,
    IValidator<DeleteExerciseSyncModel> deleteValidator) : IChangeProcessor
{
    public EntityType EntityType => EntityType.Exercise;

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

                    var newExercise = new ExerciseEntity
                    {
                        Id = data.Id,
                        MuscleGroupId = data.MuscleGroupId,
                        Title = data.Title,
                        OrderIndex = data.OrderIndex
                    };

                    await exerciseSync.AddExerciseAsync(
                        data.WorkoutId, userId, newExercise, change.Timestamp, cancellationToken);
                    break;
                }

            case ChangeAction.Update:
                {
                    var data = ChangeDataHelper.DeserializeAndValidate(change.Data, updateValidator);
                    await exerciseSync.UpdateExerciseAsync(
                        userId, data.WorkoutId, data.MuscleGroupId, data.Id, data.Title, data.OrderIndex, change.Timestamp, cancellationToken);
                    break;
                }

            case ChangeAction.Delete:
                {
                    var data = ChangeDataHelper.DeserializeAndValidate(change.Data, deleteValidator);
                    await exerciseSync.RemoveExerciseAsync(
                        userId, data.WorkoutId, data.MuscleGroupId, data.Id, change.Timestamp, cancellationToken);
                    break;
                }
        }

        response.AcknowledgedChangeIds.Add(change.Id);
    }
}
