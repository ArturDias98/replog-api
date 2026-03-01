using System.Text.Json.Serialization;

namespace replog_shared.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<ChangeAction>))]
public enum ChangeAction
{
    [JsonStringEnumMemberName("CREATE")] Create,
    [JsonStringEnumMemberName("UPDATE")] Update,
    [JsonStringEnumMemberName("DELETE")] Delete
}
