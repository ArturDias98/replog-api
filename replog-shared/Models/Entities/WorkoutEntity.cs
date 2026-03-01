namespace replog_shared.Models.Entities;

public class WorkoutEntity
{
    public required string Id { get; set; }
    public required string UserId { get; set; }
    public required string Title { get; set; }
    public required string Date { get; set; }
    public int OrderIndex { get; set; }
    public List<MuscleGroupEntity> MuscleGroup { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
