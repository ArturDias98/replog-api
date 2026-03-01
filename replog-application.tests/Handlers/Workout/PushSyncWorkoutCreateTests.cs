using Microsoft.Extensions.DependencyInjection;
using replog_application.Commands;
using replog_application.Interfaces;
using replog_application.tests.Fixtures;
using replog_application.tests.Helpers;
using replog_shared.Models.Requests;
using replog_shared.Models.Responses;

namespace replog_application.tests.Handlers.Workout;

[Collection("Application")]
public class PushSyncWorkoutCreateTests(ApplicationFixture fixture)
{
    private async Task<PushSyncResponse> HandlePushSync(string userId, List<SyncChangeDto> changes)
    {
        using var scope = fixture.Provider.CreateScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<PushSyncCommand, PushSyncResponse>>();

        return await handler.HandleAsync(new PushSyncCommand
        {
            UserId = userId,
            Request = new PushSyncRequest { Changes = changes }
        });
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateWorkout_WhenValidCreateChange()
    {
        var userId = Guid.NewGuid().ToString();
        var workoutId = Guid.NewGuid().ToString();
        var changeId = Guid.NewGuid().ToString();
        var timestamp = DateTime.UtcNow;

        var change = SyncChangeBuilder.WorkoutCreate(
            workoutId, userId, "Leg Day", "2026-03-01", 0, timestamp, changeId);

        var response = await HandlePushSync(userId, [change]);

        Assert.Contains(changeId, response.AcknowledgedChangeIds);
        Assert.Empty(response.Conflicts);
    }

    [Fact]
    public async Task HandleAsync_ShouldAcknowledgeDuplicate_WhenWorkoutIdAlreadyExists()
    {
        var userId = Guid.NewGuid().ToString();
        var workoutId = Guid.NewGuid().ToString();
        var originalTimestamp = DateTime.UtcNow.AddHours(-1);

        var createChange = SyncChangeBuilder.WorkoutCreate(
            workoutId, userId, "Original Title", "2026-03-01", 0, originalTimestamp);
        await HandlePushSync(userId, [createChange]);

        var duplicateChangeId = Guid.NewGuid().ToString();
        var duplicateChange = SyncChangeBuilder.WorkoutCreate(
            workoutId, userId, "Duplicate Title", "2026-03-02", 1,
            DateTime.UtcNow, duplicateChangeId);

        var response = await HandlePushSync(userId, [duplicateChange]);

        Assert.Contains(duplicateChangeId, response.AcknowledgedChangeIds);
        Assert.Empty(response.Conflicts);
    }
}
