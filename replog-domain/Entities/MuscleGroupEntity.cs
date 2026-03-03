namespace replog_domain.Entities;

public class MuscleGroupEntity
{
    public required string Id { get; set; }
    public required string WorkoutId { get; set; }
    public required string Title { get; set; }
    public required string Date { get; set; }
    public int OrderIndex { get; set; }
    public Dictionary<string, ExerciseEntity> Exercises { get; set; } = new();
}
