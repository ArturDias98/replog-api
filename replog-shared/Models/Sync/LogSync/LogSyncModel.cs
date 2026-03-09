namespace replog_shared.Models.Sync.LogSync;

public record LogSyncModel(
    string Id,
    string WorkoutId,
    string MuscleGroupId,
    string ExerciseId,
    int NumberReps,
    double MaxWeight,
    string Date,
    int OrderIndex);
