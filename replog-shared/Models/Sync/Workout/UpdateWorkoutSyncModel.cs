namespace replog_shared.Models.Sync.Workout;

public record UpdateWorkoutSyncModel(
    string Id,
    string Title,
    string Date,
    int OrderIndex);