using replog_infrastructure.Repositories;
using replog_infrastructure.tests.Fixtures;
using replog_shared.Models.Entities;

namespace replog_infrastructure.tests.Repositories;

[Collection("DynamoDB")]
public class WorkoutRepositoryTests(DynamoDbFixture fixture)
{
    private readonly WorkoutRepository _repository = new(fixture.Client);

    private static WorkoutEntity CreateWorkout(string userId, string? id = null) => new()
    {
        Id = id ?? Guid.NewGuid().ToString(),
        UserId = userId,
        Title = "Push Day",
        Date = "2026-03-01",
        OrderIndex = 0,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static WorkoutEntity CreateFullWorkout(string userId)
    {
        var workout = CreateWorkout(userId);
        workout.MuscleGroup =
        [
            new MuscleGroupEntity
            {
                Id = Guid.NewGuid().ToString(),
                WorkoutId = workout.Id,
                Title = "Chest",
                Date = "2026-03-01",
                OrderIndex = 0,
                Exercises =
                [
                    new ExerciseEntity
                    {
                        Id = Guid.NewGuid().ToString(),
                        MuscleGroupId = Guid.NewGuid().ToString(),
                        Title = "Bench Press",
                        OrderIndex = 0,
                        Log =
                        [
                            new LogEntity
                            {
                                Id = Guid.NewGuid().ToString(),
                                NumberReps = 10,
                                MaxWeight = 80.5,
                                Date = "2026-03-01"
                            }
                        ]
                    }
                ]
            }
        ];
        return workout;
    }

    [Fact]
    public async Task PutAndGetById_ShouldRoundtripWorkout()
    {
        var userId = Guid.NewGuid().ToString();
        var workout = CreateFullWorkout(userId);

        await _repository.PutAsync(workout);
        var result = await _repository.GetByIdAsync(workout.Id);

        Assert.NotNull(result);
        Assert.Equal(workout.Id, result.Id);
        Assert.Equal(workout.UserId, result.UserId);
        Assert.Equal(workout.Title, result.Title);
        Assert.Equal(workout.Date, result.Date);
        Assert.Equal(workout.OrderIndex, result.OrderIndex);

        var mg = Assert.Single(result.MuscleGroup);
        Assert.Equal("Chest", mg.Title);

        var exercise = Assert.Single(mg.Exercises);
        Assert.Equal("Bench Press", exercise.Title);

        var log = Assert.Single(exercise.Log);
        Assert.Equal(10, log.NumberReps);
        Assert.Equal(80.5, log.MaxWeight);
    }

    [Fact]
    public async Task GetById_ShouldReturnNull_WhenNotFound()
    {
        var result = await _repository.GetByIdAsync("nonexistent-id");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUserId_ShouldReturnOnlyUserWorkouts()
    {
        var userA = Guid.NewGuid().ToString();
        var userB = Guid.NewGuid().ToString();

        await _repository.PutAsync(CreateWorkout(userA));
        await _repository.PutAsync(CreateWorkout(userA));
        await _repository.PutAsync(CreateWorkout(userB));

        var resultsA = await _repository.GetByUserIdAsync(userA);
        var resultsB = await _repository.GetByUserIdAsync(userB);

        Assert.Equal(2, resultsA.Count);
        Assert.All(resultsA, w => Assert.Equal(userA, w.UserId));
        Assert.Single(resultsB);
        Assert.All(resultsB, w => Assert.Equal(userB, w.UserId));
    }

    [Fact]
    public async Task GetByUserId_ShouldExcludeSoftDeleted()
    {
        var userId = Guid.NewGuid().ToString();

        var active = CreateWorkout(userId);
        var deleted = CreateWorkout(userId);
        deleted.DeletedAt = DateTime.UtcNow;

        await _repository.PutAsync(active);
        await _repository.PutAsync(deleted);

        var results = await _repository.GetByUserIdAsync(userId);

        Assert.Single(results);
        Assert.Equal(active.Id, results[0].Id);
    }

    [Fact]
    public async Task GetByUserId_ShouldReturnEmptyList_WhenNoWorkouts()
    {
        var results = await _repository.GetByUserIdAsync(Guid.NewGuid().ToString());

        Assert.Empty(results);
    }

    [Fact]
    public async Task Put_ShouldOverwriteExistingWorkout()
    {
        var userId = Guid.NewGuid().ToString();
        var workout = CreateWorkout(userId);

        await _repository.PutAsync(workout);

        workout.Title = "Updated Title";
        workout.OrderIndex = 5;
        await _repository.PutAsync(workout);

        var result = await _repository.GetByIdAsync(workout.Id);

        Assert.NotNull(result);
        Assert.Equal("Updated Title", result.Title);
        Assert.Equal(5, result.OrderIndex);
    }
}
