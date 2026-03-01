namespace replog_shared.Models.Sync.LogSync;

public record DeleteLogSyncModel(
    string Id,
    string WorkoutId,
    string MuscleGroupId,
    string ExerciseId);
