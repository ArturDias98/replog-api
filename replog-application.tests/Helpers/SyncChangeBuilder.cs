using System.Text.Json;
using replog_shared.Enums;
using replog_shared.Json;
using replog_shared.Models.Requests;

namespace replog_application.tests.Helpers;

public static class SyncChangeBuilder
{
    public static SyncChangeDto WorkoutCreate(
        string id, string userId, string title = "Push Day",
        string date = "2026-03-01", int orderIndex = 0,
        DateTime? timestamp = null, string? changeId = null)
    {
        return CreateChange(
            EntityType.Workout,
            ChangeAction.Create,
            new { id, userId, title, date, orderIndex },
            timestamp, changeId);
    }

    public static SyncChangeDto WorkoutUpdate(
        string id, string title = "Updated Workout",
        string date = "2026-03-02", int orderIndex = 1,
        DateTime? timestamp = null, string? changeId = null)
    {
        return CreateChange(
            EntityType.Workout,
            ChangeAction.Update,
            new { id, title, date, orderIndex },
            timestamp, changeId);
    }

    public static SyncChangeDto WorkoutDelete(
        string id,
        DateTime? timestamp = null, string? changeId = null)
    {
        return CreateChange(
            EntityType.Workout,
            ChangeAction.Delete,
            new { id },
            timestamp, changeId);
    }

    public static JsonElement SerializeToElement<T>(T data)
    {
        var json = JsonSerializer.Serialize(data, JsonDefaults.Options);
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    private static SyncChangeDto CreateChange<T>(
        EntityType entityType,
        ChangeAction action,
        T data,
        DateTime? timestamp = null,
        string? changeId = null)
    {
        return new SyncChangeDto
        {
            Id = changeId ?? Guid.NewGuid().ToString(),
            EntityType = entityType,
            Action = action,
            Timestamp = timestamp ?? DateTime.UtcNow,
            Data = SerializeToElement(data)
        };
    }
}
