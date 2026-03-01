namespace replog_shared.Models.Sync.Exercise;

public record AddExerciseSyncModel(
    string Id,
    string WorkoutId,
    string MuscleGroupId,
    string Title,
    int OrderIndex);