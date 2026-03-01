namespace replog_shared.Models.Responses;

public class PullSyncResponse
{
    public List<WorkoutDto> Workouts { get; set; } = [];
    public DateTime ServerTimestamp { get; set; }
}
