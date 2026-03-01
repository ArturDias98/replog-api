using replog_application.Commands;
using replog_application.Queries;
using replog_shared.Models.Responses;

namespace replog_application.Interfaces;

public interface ISyncService
{
    Task<PushSyncResponse> PushAsync(PushSyncCommand command);
    Task<PullSyncResponse> PullAsync(PullSyncQuery query);
}
