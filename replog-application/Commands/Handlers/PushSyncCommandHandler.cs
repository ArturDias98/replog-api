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

    public async Task<Result<PushSyncResponse>> HandleAsync(PushSyncCommand command)
    {
        var validation = await pushValidator.ValidateAsync(command.Request);
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
            if (_processors.TryGetValue(change.EntityType, out var processor))
            {
                await processor.ProcessAsync(change, command.UserId, response);
            }
        }

        logger.LogInformation("Push sync processed {Count} change(s) for user {UserId}", command.Request.Changes.Count, command.UserId);

        return Result<PushSyncResponse>.Success(response);
    }
}
