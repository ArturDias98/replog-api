namespace replog_infrastructure.Settings;

public class DynamoDbSettings
{
    public string Region { get; set; } = "us-east-1";
    public string? ServiceURL { get; set; }
    public string TableName { get; set; } = "replog-workouts";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}
