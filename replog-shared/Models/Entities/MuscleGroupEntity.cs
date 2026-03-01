namespace replog_shared.Models.Entities;

public class MuscleGroupEntity
{
    public required string Id { get; set; }
    public required string WorkoutId { get; set; }
    public required string Title { get; set; }
    public required string Date { get; set; }
    public int OrderIndex { get; set; }
    public List<ExerciseEntity> Exercises { get; set; } = [];
}
