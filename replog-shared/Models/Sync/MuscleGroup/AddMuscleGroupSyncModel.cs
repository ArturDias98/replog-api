namespace replog_shared.Models.Sync.MuscleGroup;

public record AddMuscleGroupSyncModel(
    string Id,
    string WorkoutId,
    string Title,
    string Date,
    int OrderIndex);
