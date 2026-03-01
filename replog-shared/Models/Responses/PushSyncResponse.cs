namespace replog_shared.Models.Responses;

public class PushSyncResponse
{
    public List<string> AcknowledgedChangeIds { get; set; } = [];
    public List<ConflictDto> Conflicts { get; set; } = [];
    public DateTime ServerTimestamp { get; set; }
}

public class ConflictDto
{
    public required string ChangeId { get; set; }
    public string Resolution { get; set; } = "server_wins";
    public object? ServerVersion { get; set; }
}
