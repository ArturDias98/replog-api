using replog_shared.Enums;
using replog_shared.Models.Entities;
using replog_shared.Models.Requests;
using replog_shared.Models.Responses;

namespace replog_application.Interfaces;

public interface IChangeProcessor
{
    EntityType EntityType { get; }

    void Process(
        SyncChangeDto change,
        string userId,
        Dictionary<string, WorkoutEntity> workoutCache,
        HashSet<string> dirtyWorkouts,
        PushSyncResponse response);
}
