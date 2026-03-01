using System.Text.Json;
using replog_shared.Enums;

namespace replog_shared.Models.Requests;

public class PushSyncRequest
{
    public required List<SyncChangeDto> Changes { get; set; }
    public DateTime? LastSyncedAt { get; set; }
}

public class SyncChangeDto
{
    public required string Id { get; set; }
    public required EntityType EntityType { get; set; }
    public required ChangeAction Action { get; set; }
    public required DateTime Timestamp { get; set; }
    public JsonElement? Data { get; set; }
}
