using System.Text.Json.Serialization;

namespace replog_shared.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<EntityType>))]
public enum EntityType
{
    [JsonStringEnumMemberName("workout")] Workout,
    [JsonStringEnumMemberName("muscleGroup")] MuscleGroup,
    [JsonStringEnumMemberName("exercise")] Exercise,
    [JsonStringEnumMemberName("log")] Log
}
