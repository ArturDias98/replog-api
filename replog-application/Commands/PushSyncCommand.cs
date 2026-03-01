using replog_shared.Models.Requests;

namespace replog_application.Commands;

public class PushSyncCommand
{
    public required string UserId { get; set; }
    public required PushSyncRequest Request { get; set; }
}
