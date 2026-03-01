namespace replog_shared.Models.Sync.Exercise;

public record DeleteExerciseSyncModel(
    string Id,
    string WorkoutId,
    string MuscleGroupId);
