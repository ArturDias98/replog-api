using Microsoft.Extensions.DependencyInjection;
using replog_application.Commands;
using replog_application.Interfaces;
using replog_application.tests.Fixtures;
using replog_application.tests.Helpers;
using replog_shared.Models.Requests;
using replog_shared.Models.Responses;

namespace replog_application.tests.Handlers.Workout;

[Collection("Application")]
public class PushSyncWorkoutDeleteTests(ApplicationFixture fixture)
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
    public async Task HandleAsync_ShouldSoftDeleteWorkout_WhenValidDeleteChange()
    {
        var userId = Guid.NewGuid().ToString();
        var workoutId = Guid.NewGuid().ToString();
        var createTime = DateTime.UtcNow.AddHours(-1);

        await HandlePushSync(userId, [
            SyncChangeBuilder.WorkoutCreate(workoutId, userId, "Push Day", "2026-03-01", 0, createTime)
        ]);

        var deleteChangeId = Guid.NewGuid().ToString();
        var deleteChange = SyncChangeBuilder.WorkoutDelete(workoutId, DateTime.UtcNow, deleteChangeId);

        var response = await HandlePushSync(userId, [deleteChange]);

        Assert.Contains(deleteChangeId, response.AcknowledgedChangeIds);
        Assert.Empty(response.Conflicts);

        // Verify workout no longer appears in pull sync
        using var scope = fixture.Provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkoutRepository>();
        var userWorkouts = await repository.GetByUserIdAsync(userId);
        Assert.DoesNotContain(userWorkouts, w => w.Id == workoutId);
    }

    [Fact]
    public async Task HandleAsync_ShouldAcknowledgeDelete_WhenWorkoutAlreadyDeleted()
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

        var secondDeleteChangeId = Guid.NewGuid().ToString();
        var secondDelete = SyncChangeBuilder.WorkoutDelete(workoutId, DateTime.UtcNow, secondDeleteChangeId);

        var response = await HandlePushSync(userId, [secondDelete]);

        Assert.Contains(secondDeleteChangeId, response.AcknowledgedChangeIds);
        Assert.Empty(response.Conflicts);
    }

    [Fact]
    public async Task HandleAsync_ShouldAcknowledgeDelete_WhenWorkoutDoesNotExist()
    {
        var userId = Guid.NewGuid().ToString();
        var deleteChangeId = Guid.NewGuid().ToString();
        var deleteChange = SyncChangeBuilder.WorkoutDelete(
            Guid.NewGuid().ToString(), DateTime.UtcNow, deleteChangeId);

        var response = await HandlePushSync(userId, [deleteChange]);

        Assert.Contains(deleteChangeId, response.AcknowledgedChangeIds);
        Assert.Empty(response.Conflicts);
    }
}
