using replog_infrastructure.Repositories;
using replog_domain.Entities;
using replog_tests_shared.Comparers;
using replog_tests_shared.Fixtures;

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
        var mgId = Guid.NewGuid().ToString();
        var exId = Guid.NewGuid().ToString();
        var logId = Guid.NewGuid().ToString();

        workout.MuscleGroup = new Dictionary<string, MuscleGroupEntity>
        {
            [mgId] = new MuscleGroupEntity
            {
                Id = mgId,
                WorkoutId = workout.Id,
                Title = "Chest",
                Date = "2026-03-01",
                OrderIndex = 0,
                Exercises = new Dictionary<string, ExerciseEntity>
                {
                    [exId] = new ExerciseEntity
                    {
                        Id = exId,
                        MuscleGroupId = mgId,
                        Title = "Bench Press",
                        OrderIndex = 0,
                        Log = new Dictionary<string, LogEntity>
                        {
                            [logId] = new LogEntity
                            {
                                Id = logId,
                                NumberReps = 10,
                                MaxWeight = 80.5,
                                Date = "2026-03-01",
                                OrderIndex = 0
                            }
                        }
                    }
                }
            }
        };
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
        Assert.Equal(workout, result, WorkoutEntityComparer.Instance);
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

    [Fact]
    public async Task PutAndGetById_ShouldPreserveMultipleMuscleGroups()
    {
        var userId = Guid.NewGuid().ToString();
        var workout = CreateWorkout(userId);
        var mg1Id = Guid.NewGuid().ToString();
        var mg2Id = Guid.NewGuid().ToString();

        workout.MuscleGroup = new Dictionary<string, MuscleGroupEntity>
        {
            [mg1Id] = new MuscleGroupEntity
            {
                Id = mg1Id,
                WorkoutId = workout.Id,
                Title = "Chest",
                Date = "2026-03-01",
                OrderIndex = 0
            },
            [mg2Id] = new MuscleGroupEntity
            {
                Id = mg2Id,
                WorkoutId = workout.Id,
                Title = "Back",
                Date = "2026-03-01",
                OrderIndex = 1
            }
        };

        await _repository.PutAsync(workout);
        var result = await _repository.GetByIdAsync(workout.Id);

        Assert.NotNull(result);
        Assert.Equal(workout, result, WorkoutEntityComparer.Instance);
    }

    [Fact]
    public async Task PutAndGetById_ShouldPreserveMultipleExercisesPerMuscleGroup()
    {
        var userId = Guid.NewGuid().ToString();
        var workout = CreateWorkout(userId);
        var mgId = Guid.NewGuid().ToString();
        var ex1Id = Guid.NewGuid().ToString();
        var ex2Id = Guid.NewGuid().ToString();

        workout.MuscleGroup = new Dictionary<string, MuscleGroupEntity>
        {
            [mgId] = new MuscleGroupEntity
            {
                Id = mgId,
                WorkoutId = workout.Id,
                Title = "Chest",
                Date = "2026-03-01",
                OrderIndex = 0,
                Exercises = new Dictionary<string, ExerciseEntity>
                {
                    [ex1Id] = new ExerciseEntity
                    {
                        Id = ex1Id,
                        MuscleGroupId = mgId,
                        Title = "Bench Press",
                        OrderIndex = 0
                    },
                    [ex2Id] = new ExerciseEntity
                    {
                        Id = ex2Id,
                        MuscleGroupId = mgId,
                        Title = "Incline Press",
                        OrderIndex = 1
                    }
                }
            }
        };

        await _repository.PutAsync(workout);
        var result = await _repository.GetByIdAsync(workout.Id);

        Assert.NotNull(result);
        Assert.Equal(workout, result, WorkoutEntityComparer.Instance);
    }

    [Fact]
    public async Task PutAndGetById_ShouldPreserveMultipleLogsPerExercise()
    {
        var userId = Guid.NewGuid().ToString();
        var workout = CreateWorkout(userId);
        var mgId = Guid.NewGuid().ToString();
        var exId = Guid.NewGuid().ToString();
        var log1Id = Guid.NewGuid().ToString();
        var log2Id = Guid.NewGuid().ToString();

        workout.MuscleGroup = new Dictionary<string, MuscleGroupEntity>
        {
            [mgId] = new MuscleGroupEntity
            {
                Id = mgId,
                WorkoutId = workout.Id,
                Title = "Chest",
                Date = "2026-03-01",
                OrderIndex = 0,
                Exercises = new Dictionary<string, ExerciseEntity>
                {
                    [exId] = new ExerciseEntity
                    {
                        Id = exId,
                        MuscleGroupId = mgId,
                        Title = "Bench Press",
                        OrderIndex = 0,
                        Log = new Dictionary<string, LogEntity>
                        {
                            [log1Id] = new LogEntity
                            {
                                Id = log1Id,
                                NumberReps = 10,
                                MaxWeight = 80.0,
                                Date = "2026-03-01",
                                OrderIndex = 0
                            },
                            [log2Id] = new LogEntity
                            {
                                Id = log2Id,
                                NumberReps = 8,
                                MaxWeight = 90.0,
                                Date = "2026-03-02",
                                OrderIndex = 1
                            }
                        }
                    }
                }
            }
        };

        await _repository.PutAsync(workout);
        var result = await _repository.GetByIdAsync(workout.Id);

        Assert.NotNull(result);
        Assert.Equal(workout, result, WorkoutEntityComparer.Instance);
    }

    [Fact]
    public async Task PutAndGetById_ShouldPreserveComplexNestedStructure()
    {
        var userId = Guid.NewGuid().ToString();
        var workout = CreateWorkout(userId);
        var mg1Id = Guid.NewGuid().ToString();
        var mg2Id = Guid.NewGuid().ToString();
        var ex1Id = Guid.NewGuid().ToString();
        var ex2Id = Guid.NewGuid().ToString();
        var ex3Id = Guid.NewGuid().ToString();
        var ex4Id = Guid.NewGuid().ToString();
        var log1Id = Guid.NewGuid().ToString();
        var log2Id = Guid.NewGuid().ToString();
        var log3Id = Guid.NewGuid().ToString();
        var log4Id = Guid.NewGuid().ToString();

        workout.MuscleGroup = new Dictionary<string, MuscleGroupEntity>
        {
            [mg1Id] = new MuscleGroupEntity
            {
                Id = mg1Id,
                WorkoutId = workout.Id,
                Title = "Chest",
                Date = "2026-03-01",
                OrderIndex = 0,
                Exercises = new Dictionary<string, ExerciseEntity>
                {
                    [ex1Id] = new ExerciseEntity
                    {
                        Id = ex1Id,
                        MuscleGroupId = mg1Id,
                        Title = "Bench Press",
                        OrderIndex = 0,
                        Log = new Dictionary<string, LogEntity>
                        {
                            [log1Id] = new LogEntity { Id = log1Id, NumberReps = 10, MaxWeight = 80, Date = "2026-03-01", OrderIndex = 0 },
                            [log2Id] = new LogEntity { Id = log2Id, NumberReps = 8, MaxWeight = 85, Date = "2026-03-01", OrderIndex = 1 }
                        }
                    },
                    [ex2Id] = new ExerciseEntity
                    {
                        Id = ex2Id,
                        MuscleGroupId = mg1Id,
                        Title = "Flyes",
                        OrderIndex = 1
                    }
                }
            },
            [mg2Id] = new MuscleGroupEntity
            {
                Id = mg2Id,
                WorkoutId = workout.Id,
                Title = "Triceps",
                Date = "2026-03-01",
                OrderIndex = 1,
                Exercises = new Dictionary<string, ExerciseEntity>
                {
                    [ex3Id] = new ExerciseEntity
                    {
                        Id = ex3Id,
                        MuscleGroupId = mg2Id,
                        Title = "Dips",
                        OrderIndex = 0,
                        Log = new Dictionary<string, LogEntity>
                        {
                            [log3Id] = new LogEntity { Id = log3Id, NumberReps = 12, MaxWeight = 0, Date = "2026-03-01", OrderIndex = 0 }
                        }
                    },
                    [ex4Id] = new ExerciseEntity
                    {
                        Id = ex4Id,
                        MuscleGroupId = mg2Id,
                        Title = "Pushdowns",
                        OrderIndex = 1,
                        Log = new Dictionary<string, LogEntity>
                        {
                            [log4Id] = new LogEntity { Id = log4Id, NumberReps = 15, MaxWeight = 30, Date = "2026-03-01", OrderIndex = 0 }
                        }
                    }
                }
            }
        };

        await _repository.PutAsync(workout);
        var result = await _repository.GetByIdAsync(workout.Id);

        Assert.NotNull(result);
        Assert.Equal(workout, result, WorkoutEntityComparer.Instance);
    }

    [Fact]
    public async Task PutAndGetById_ShouldHandleEmptyNestedDictionaries()
    {
        var userId = Guid.NewGuid().ToString();
        var mgId = Guid.NewGuid().ToString();
        var exId = Guid.NewGuid().ToString();

        // Workout with empty MuscleGroup
        var emptyWorkout = CreateWorkout(userId);
        await _repository.PutAsync(emptyWorkout);
        var result1 = await _repository.GetByIdAsync(emptyWorkout.Id);
        Assert.NotNull(result1);
        Assert.Equal(emptyWorkout, result1, WorkoutEntityComparer.Instance);

        // MuscleGroup with empty Exercises
        var workoutEmptyExercises = CreateWorkout(userId);
        workoutEmptyExercises.MuscleGroup = new Dictionary<string, MuscleGroupEntity>
        {
            [mgId] = new MuscleGroupEntity
            {
                Id = mgId,
                WorkoutId = workoutEmptyExercises.Id,
                Title = "Chest",
                Date = "2026-03-01",
                OrderIndex = 0
            }
        };
        await _repository.PutAsync(workoutEmptyExercises);
        var result2 = await _repository.GetByIdAsync(workoutEmptyExercises.Id);
        Assert.NotNull(result2);
        Assert.Equal(workoutEmptyExercises, result2, WorkoutEntityComparer.Instance);

        // Exercise with empty Log
        var workoutEmptyLogs = CreateWorkout(userId);
        workoutEmptyLogs.MuscleGroup = new Dictionary<string, MuscleGroupEntity>
        {
            [mgId] = new MuscleGroupEntity
            {
                Id = mgId,
                WorkoutId = workoutEmptyLogs.Id,
                Title = "Chest",
                Date = "2026-03-01",
                OrderIndex = 0,
                Exercises = new Dictionary<string, ExerciseEntity>
                {
                    [exId] = new ExerciseEntity
                    {
                        Id = exId,
                        MuscleGroupId = mgId,
                        Title = "Bench Press",
                        OrderIndex = 0
                    }
                }
            }
        };
        await _repository.PutAsync(workoutEmptyLogs);
        var result3 = await _repository.GetByIdAsync(workoutEmptyLogs.Id);
        Assert.NotNull(result3);
        Assert.Equal(workoutEmptyLogs, result3, WorkoutEntityComparer.Instance);
    }

    [Fact]
    public async Task Put_ShouldUpdateNestedDataOnOverwrite()
    {
        var userId = Guid.NewGuid().ToString();
        var workout = CreateWorkout(userId);
        var mg1Id = Guid.NewGuid().ToString();

        workout.MuscleGroup = new Dictionary<string, MuscleGroupEntity>
        {
            [mg1Id] = new MuscleGroupEntity
            {
                Id = mg1Id,
                WorkoutId = workout.Id,
                Title = "Chest",
                Date = "2026-03-01",
                OrderIndex = 0
            }
        };

        await _repository.PutAsync(workout);

        // Add a second muscle group and re-put
        var mg2Id = Guid.NewGuid().ToString();
        workout.MuscleGroup[mg2Id] = new MuscleGroupEntity
        {
            Id = mg2Id,
            WorkoutId = workout.Id,
            Title = "Back",
            Date = "2026-03-01",
            OrderIndex = 1
        };

        await _repository.PutAsync(workout);
        var result = await _repository.GetByIdAsync(workout.Id);

        Assert.NotNull(result);
        Assert.Equal(workout, result, WorkoutEntityComparer.Instance);
    }
}
