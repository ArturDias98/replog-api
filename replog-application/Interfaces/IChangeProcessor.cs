using replog_shared.Enums;
using replog_shared.Models.Requests;
using replog_shared.Models.Responses;

namespace replog_application.Interfaces;

public interface IChangeProcessor
{
    EntityType EntityType { get; }

    Task ProcessAsync(
        SyncChangeDto change,
        string userId,
        PushSyncResponse response);
}
