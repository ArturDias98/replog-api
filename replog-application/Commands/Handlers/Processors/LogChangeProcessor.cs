using FluentValidation;
using replog_application.Interfaces;
using replog_application.Interfaces.SyncOperations;
using replog_shared.Enums;
using replog_domain.Entities;
using replog_shared.Models.Requests;
using replog_shared.Models.Responses;
using replog_shared.Models.Sync.LogSync;

namespace replog_application.Commands.Handlers.Processors;

public class LogChangeProcessor(
    ILogSyncRepository logSync,
    IValidator<LogSyncModel> addValidator,
    IValidator<UpdateLogSyncModel> updateValidator,
    IValidator<DeleteLogSyncModel> deleteValidator) : IChangeProcessor
{
    public EntityType EntityType => EntityType.Log;

    public async Task ProcessAsync(
        SyncChangeDto change,
        string userId,
        PushSyncResponse response)
    {
        switch (change.Action)
        {
            case ChangeAction.Create:
            {
                var data = ChangeDataHelper.DeserializeAndValidate(change.Data, addValidator);

                var newLog = new LogEntity
                {
                    Id = data.Id,
                    NumberReps = data.NumberReps,
                    MaxWeight = data.MaxWeight,
                    Date = data.Date
                };

                await logSync.AddLogAsync(
                    data.WorkoutId, userId, data.MuscleGroupId, data.ExerciseId, newLog, change.Timestamp);
                break;
            }

            case ChangeAction.Update:
            {
                var data = ChangeDataHelper.DeserializeAndValidate(change.Data, updateValidator);
                await logSync.UpdateLogAsync(
                    data.WorkoutId, data.MuscleGroupId, data.ExerciseId, data.Id,
                    data.NumberReps, data.MaxWeight, change.Timestamp);
                break;
            }

            case ChangeAction.Delete:
            {
                var data = ChangeDataHelper.DeserializeAndValidate(change.Data, deleteValidator);
                await logSync.RemoveLogAsync(
                    data.WorkoutId, data.MuscleGroupId, data.ExerciseId, data.Id, change.Timestamp);
                break;
            }
            default:
                throw new Exception("Invalid operation");
        }

        response.AcknowledgedChangeIds.Add(change.Id);
    }
}
