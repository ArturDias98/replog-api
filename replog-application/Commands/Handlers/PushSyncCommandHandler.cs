using FluentValidation;
using replog_application.Interfaces;
using replog_shared.Enums;
using replog_shared.Models.Requests;
using replog_shared.Models.Responses;

namespace replog_application.Commands.Handlers;

public class PushSyncCommandHandler(
    IValidator<PushSyncRequest> pushValidator,
    IEnumerable<IChangeProcessor> changeProcessors) : ICommandHandler<PushSyncCommand, PushSyncResponse>
{
    private readonly Dictionary<EntityType, IChangeProcessor> _processors =
        changeProcessors.ToDictionary(p => p.EntityType);

    public async Task<PushSyncResponse> HandleAsync(PushSyncCommand command)
    {
        await pushValidator.ValidateAndThrowAsync(command.Request);

        var response = new PushSyncResponse { ServerTimestamp = DateTime.UtcNow };

        var orderedChanges = command.Request.Changes.OrderBy(c => c.Timestamp);

        foreach (var change in orderedChanges)
        {
            if (_processors.TryGetValue(change.EntityType, out var processor))
            {
                await processor.ProcessAsync(change, command.UserId, response);
            }
        }

        return response;
    }
}
