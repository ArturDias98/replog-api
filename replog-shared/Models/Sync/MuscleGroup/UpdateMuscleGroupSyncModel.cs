namespace replog_shared.Models.Sync.MuscleGroup;

public record UpdateMuscleGroupSyncModel(
    string Id,
    string WorkoutId,
    string Title,
    string Date,
    int OrderIndex);