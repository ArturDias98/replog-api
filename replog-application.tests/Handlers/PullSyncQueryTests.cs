using Microsoft.Extensions.DependencyInjection;
using replog_application.Commands;
using replog_application.Interfaces;
using replog_application.Queries;
using replog_application.tests.Fixtures;
using replog_application.tests.Helpers;
using replog_shared.Models.Requests;
using replog_shared.Models.Responses;

namespace replog_application.tests.Handlers;

[Collection("Application")]
public class PullSyncQueryTests(ApplicationFixture fixture)
{
    private async Task<Result<PullSyncResponse>> HandlePullSync(string userId)
    {
        using var scope = fixture.Provider.CreateScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<IQueryHandler<PullSyncQuery, Result<PullSyncResponse>>>();

        return await handler.HandleAsync(new PullSyncQuery { UserId = userId });
    }

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
    public async Task HandleAsync_ShouldReturnAllUserWorkouts_WhenWorkoutsExist()
    {
        var userId = Guid.NewGuid().ToString();

        await HandlePushSync(userId, [
            SyncChangeBuilder.WorkoutCreate(Guid.NewGuid().ToString(), userId, "Push Day", "2026-03-01", 0),
            SyncChangeBuilder.WorkoutCreate(Guid.NewGuid().ToString(), userId, "Pull Day", "2026-03-02", 1)
        ]);

        var result = await HandlePullSync(userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Workouts.Count);
        Assert.Contains(result.Value.Workouts, w => w.Title == "Push Day");
        Assert.Contains(result.Value.Workouts, w => w.Title == "Pull Day");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyList_WhenNoWorkoutsExist()
    {
        var userId = Guid.NewGuid().ToString();

        var result = await HandlePullSync(userId);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Workouts);
    }

    [Fact]
    public async Task HandleAsync_ShouldExcludeSoftDeletedWorkouts()
    {
        var userId = Guid.NewGuid().ToString();
        var activeWorkoutId = Guid.NewGuid().ToString();
        var deletedWorkoutId = Guid.NewGuid().ToString();
        var createTime = DateTime.UtcNow.AddHours(-1);

        await HandlePushSync(userId, [
            SyncChangeBuilder.WorkoutCreate(activeWorkoutId, userId, "Active Workout", "2026-03-01", 0, createTime),
            SyncChangeBuilder.WorkoutCreate(deletedWorkoutId, userId, "To Be Deleted", "2026-03-02", 1, createTime)
        ]);

        await HandlePushSync(userId, [
            SyncChangeBuilder.WorkoutDelete(deletedWorkoutId, DateTime.UtcNow)
        ]);

        var result = await HandlePullSync(userId);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Workouts);
        Assert.Equal("Active Workout", result.Value.Workouts[0].Title);
    }
}
