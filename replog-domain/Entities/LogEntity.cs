namespace replog_domain.Entities;

public class LogEntity
{
    public required string Id { get; set; }
    public int NumberReps { get; set; }
    public double MaxWeight { get; set; }
    public required string Date { get; set; }
    public int OrderIndex { get; set; }
}
