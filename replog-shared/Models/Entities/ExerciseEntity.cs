namespace replog_shared.Models.Entities;

public class ExerciseEntity
{
    public required string Id { get; set; }
    public required string MuscleGroupId { get; set; }
    public required string Title { get; set; }
    public int OrderIndex { get; set; }
    public List<LogEntity> Log { get; set; } = [];
}
