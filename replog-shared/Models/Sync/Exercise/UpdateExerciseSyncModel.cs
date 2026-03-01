namespace replog_shared.Models.Sync.Exercise;

public record UpdateExerciseSyncModel(
    string Id,
    string WorkoutId,
    string MuscleGroupId,
    string Title,
    int OrderIndex);