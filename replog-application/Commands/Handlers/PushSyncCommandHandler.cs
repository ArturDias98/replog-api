using FluentValidation;
using Microsoft.Extensions.Logging;
using replog_application.Interfaces;
using replog_shared.Enums;
using replog_shared.Models.Requests;
using replog_shared.Models.Responses;

namespace replog_application.Commands.Handlers;

public class PushSyncCommandHandler(
    IValidator<PushSyncRequest> pushValidator,
    IEnumerable<IChangeProcessor> changeProcessors,
    ILogger<PushSyncCommandHandler> logger) : ICommandHandler<PushSyncCommand, Result<PushSyncResponse>>
{
    private readonly Dictionary<EntityType, IChangeProcessor> _processors =
        changeProcessors.ToDictionary(p => p.EntityType);

    public async Task<Result<PushSyncResponse>> HandleAsync(PushSyncCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await pushValidator.ValidateAsync(command.Request, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            logger.LogWarning("Push sync validation failed for user {UserId}: {Errors}", command.UserId, errors);
            return Result<PushSyncResponse>.Failure("validation_error", errors);
        }

        var response = new PushSyncResponse { ServerTimestamp = DateTime.UtcNow };

        var orderedChanges = command.Request.Changes.OrderBy(c => c.Timestamp);

        foreach (var change in orderedChanges)
        {
            try
            {
                if (_processors.TryGetValue(change.EntityType, out var processor))
                    await processor.ProcessAsync(change, command.UserId, response, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Change {ChangeId} ({EntityType}/{Action}) failed for user {UserId}",
                    change.Id, change.EntityType, change.Action, command.UserId);
                response.FailedChangeIds.Add(change.Id);
            }
        }

        logger.LogInformation(
            "Push sync for user {UserId}: {Acknowledged} acknowledged, {Failed} failed, {Conflicts} conflicts",
            command.UserId,
            response.AcknowledgedChangeIds.Count,
            response.FailedChangeIds.Count,
            response.Conflicts.Count);

        return Result<PushSyncResponse>.Success(response);
    }
}
