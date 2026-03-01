namespace replog_shared.Models.Sync.Workout;

public record AddWorkoutSyncModel(
    string Id,
    string UserId,
    string Title,
    string Date,
    int OrderIndex);