namespace replog_shared.Models.Sync.LogSync;

public record UpdateLogSyncModel(
    string Id,
    string WorkoutId,
    string MuscleGroupId,
    string ExerciseId,
    int NumberReps,
    int MaxWeight);