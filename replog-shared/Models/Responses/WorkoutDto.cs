namespace replog_shared.Models.Responses;

public class WorkoutDto
{
    public required string Id { get; set; }
    public required string UserId { get; set; }
    public required string Title { get; set; }
    public required string Date { get; set; }
    public int OrderIndex { get; set; }
    public List<MuscleGroupDto> MuscleGroup { get; set; } = [];
}

public class MuscleGroupDto
{
    public required string Id { get; set; }
    public required string WorkoutId { get; set; }
    public required string Title { get; set; }
    public required string Date { get; set; }
    public int OrderIndex { get; set; }
    public List<ExerciseDto> Exercises { get; set; } = [];
}

public class ExerciseDto
{
    public required string Id { get; set; }
    public required string MuscleGroupId { get; set; }
    public required string Title { get; set; }
    public int OrderIndex { get; set; }
    public List<LogDto> Log { get; set; } = [];
}

public class LogDto
{
    public required string Id { get; set; }
    public int NumberReps { get; set; }
    public double MaxWeight { get; set; }
    public required string Date { get; set; }
    public int OrderIndex { get; set; }
}
