using Microsoft.Extensions.DependencyInjection;
using replog_application.Commands;
using replog_application.Interfaces;
using replog_application.tests.Fixtures;
using replog_application.tests.Helpers;
using replog_shared.Models.Requests;
using replog_shared.Models.Responses;

namespace replog_application.tests.Handlers.Workout;

[Collection("Application")]
public class PushSyncWorkoutUpdateTests(ApplicationFixture fixture)
{
    private async Task<Result<PushSyncResponse>> HandlePushSync(string userId, List<SyncChangeDto> changes)
    {
        using var scope = fixture.Provider.CreateScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<PushSyncCommand, Result<PushSyncResponse>>>();

        return await handler.HandleAsync(new PushSyncCommand
        {
            UserId = userId,
            Request = new PushSyncRequest { Changes = changes }
        });
    }

    [Fact]
    public async Task HandleAsync_ShouldUpdateWorkout_WhenValidUpdateChange()
    {
        var userId = Guid.NewGuid().ToString();
        var workoutId = Guid.NewGuid().ToString();
        var createTime = DateTime.UtcNow.AddHours(-1);

        await HandlePushSync(userId, [
            SyncChangeBuilder.WorkoutCreate(workoutId, userId, "Push Day", "2026-03-01", 0, createTime)
        ]);

        var updateTime = DateTime.UtcNow;
        var updateChangeId = Guid.NewGuid().ToString();
        var updateChange = SyncChangeBuilder.WorkoutUpdate(
            workoutId, "Pull Day", "2026-03-02", 1, updateTime, updateChangeId);

        var result = await HandlePushSync(userId, [updateChange]);

        Assert.True(result.IsSuccess);
        Assert.Contains(updateChangeId, result.Value!.AcknowledgedChangeIds);
        Assert.Empty(result.Value.Conflicts);
    }

    [Fact]
    public async Task HandleAsync_ShouldAcknowledgeUpdate_WhenWorkoutIsDeleted()
    {
        var userId = Guid.NewGuid().ToString();
        var workoutId = Guid.NewGuid().ToString();
        var createTime = DateTime.UtcNow.AddHours(-2);
        var deleteTime = DateTime.UtcNow.AddHours(-1);

        await HandlePushSync(userId, [
            SyncChangeBuilder.WorkoutCreate(workoutId, userId, "Push Day", "2026-03-01", 0, createTime)
        ]);
        await HandlePushSync(userId, [
            SyncChangeBuilder.WorkoutDelete(workoutId, deleteTime)
        ]);

        var updateChangeId = Guid.NewGuid().ToString();
        var updateChange = SyncChangeBuilder.WorkoutUpdate(
            workoutId, "Updated Title", "2026-03-02", 1, DateTime.UtcNow, updateChangeId);

        var result = await HandlePushSync(userId, [updateChange]);

        Assert.True(result.IsSuccess);
        Assert.Contains(updateChangeId, result.Value!.AcknowledgedChangeIds);
        Assert.Empty(result.Value.Conflicts);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenServerVersionIsNewer()
    {
        var userId = Guid.NewGuid().ToString();
        var workoutId = Guid.NewGuid().ToString();
        var createTime = DateTime.UtcNow.AddHours(-2);
        var serverUpdateTime = DateTime.UtcNow.AddHours(-1);

        await HandlePushSync(userId, [
            SyncChangeBuilder.WorkoutCreate(workoutId, userId, "Push Day", "2026-03-01", 0, createTime)
        ]);

        await HandlePushSync(userId, [
            SyncChangeBuilder.WorkoutUpdate(workoutId, "Server Version", "2026-03-02", 1, serverUpdateTime)
        ]);

        var clientUpdateChangeId = Guid.NewGuid().ToString();
        var clientUpdateTime = createTime.AddMinutes(30);
        var conflictChange = SyncChangeBuilder.WorkoutUpdate(
            workoutId, "Client Version", "2026-03-03", 2, clientUpdateTime, clientUpdateChangeId);

        var result = await HandlePushSync(userId, [conflictChange]);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(clientUpdateChangeId, result.Value!.AcknowledgedChangeIds);
        Assert.Single(result.Value.Conflicts);
        Assert.Equal(clientUpdateChangeId, result.Value.Conflicts[0].ChangeId);
        Assert.Equal("server_wins", result.Value.Conflicts[0].Resolution);
    }

    [Fact]
    public async Task HandleAsync_ShouldAcknowledgeUpdate_WhenWorkoutDoesNotExist()
    {
        var userId = Guid.NewGuid().ToString();
        var nonexistentWorkoutId = Guid.NewGuid().ToString();
        var updateChangeId = Guid.NewGuid().ToString();

        var updateChange = SyncChangeBuilder.WorkoutUpdate(
            nonexistentWorkoutId, "Some Title", "2026-03-01", 0, DateTime.UtcNow, updateChangeId);

        var result = await HandlePushSync(userId, [updateChange]);

        Assert.True(result.IsSuccess);
        Assert.Contains(updateChangeId, result.Value!.AcknowledgedChangeIds);
        Assert.Empty(result.Value.Conflicts);
    }
}
