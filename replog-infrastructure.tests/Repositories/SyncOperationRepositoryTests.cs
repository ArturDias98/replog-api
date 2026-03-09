using Microsoft.Extensions.Options;
using replog_infrastructure.Repositories;
using replog_infrastructure.Repositories.SyncOperations;
using replog_infrastructure.Settings;
using replog_domain.Entities;
using replog_tests_shared.Comparers;
using replog_tests_shared.Fixtures;

namespace replog_infrastructure.tests.Repositories;

[Collection("DynamoDB")]
public class WorkoutSyncRepositoryTests(DynamoDbFixture fixture)
{
    private static readonly IOptions<DynamoDbSettings> Settings = Options.Create(new DynamoDbSettings());
    private readonly WorkoutSyncRepository _workoutSync = new(fixture.Client, Settings);
    private readonly WorkoutRepository _workoutRepo = new(fixture.Client, Settings);

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

    // ── Workout Create ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateWorkout_ShouldReturnTrue_WhenNewWorkout()
    {
        var workout = CreateWorkout(Guid.NewGuid().ToString());

        var result = await _workoutSync.CreateWorkoutAsync(workout);

        Assert.True(result);

        var fetched = await _workoutRepo.GetByIdAsync(workout.Id);
        Assert.NotNull(fetched);
        Assert.Equal(workout, fetched, WorkoutEntityComparer.Instance);
    }

    [Fact]
    public async Task CreateWorkout_ShouldReturnFalse_WhenDuplicate()
    {
        var workout = CreateWorkout(Guid.NewGuid().ToString());
        await _workoutSync.CreateWorkoutAsync(workout);

        var result = await _workoutSync.CreateWorkoutAsync(workout);

        Assert.False(result);
    }

    // ── Workout Update ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateWorkout_ShouldReturnNull_WhenSuccessful()
    {
        var userId = Guid.NewGuid().ToString();
        var workout = CreateWorkout(userId);
        await _workoutSync.CreateWorkoutAsync(workout);

        var newTimestamp = DateTime.UtcNow.AddMinutes(1);
        var conflict = await _workoutSync.UpdateWorkoutAsync(
            userId, workout.Id, "Updated Title", "2026-04-01", 5, newTimestamp);

        Assert.Null(conflict);

        var fetched = await _workoutRepo.GetByIdAsync(workout.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Updated Title", fetched.Title);
        Assert.Equal("2026-04-01", fetched.Date);
        Assert.Equal(5, fetched.OrderIndex);
    }

    [Fact]
    public async Task UpdateWorkout_ShouldReturnConflict_WhenServerVersionIsNewer()
    {
        var userId = Guid.NewGuid().ToString();
        var workout = CreateWorkout(userId);
        workout.UpdatedAt = DateTime.UtcNow;
        await _workoutSync.CreateWorkoutAsync(workout);

        // Update with server time
        var serverTime = DateTime.UtcNow.AddMinutes(5);
        await _workoutSync.UpdateWorkoutAsync(userId, workout.Id, "Server Title", "2026-04-01", 1, serverTime);

        // Try client update with older timestamp
        var clientTime = workout.UpdatedAt.AddMinutes(1);
        var conflict = await _workoutSync.UpdateWorkoutAsync(
            userId, workout.Id, "Client Title", "2026-05-01", 2, clientTime);

        Assert.NotNull(conflict);
        Assert.Equal("Server Title", conflict.Title);
    }

    [Fact]
    public async Task UpdateWorkout_ShouldReturnNull_WhenWorkoutDoesNotExist()
    {
        var result = await _workoutSync.UpdateWorkoutAsync(
            Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "Title", "2026-03-01", 0, DateTime.UtcNow);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateWorkout_ShouldReturnNull_WhenWorkoutIsDeleted()
    {
        var userId = Guid.NewGuid().ToString();
        var workout = CreateWorkout(userId);
        await _workoutSync.CreateWorkoutAsync(workout);
        await _workoutSync.SoftDeleteWorkoutAsync(userId, workout.Id, DateTime.UtcNow);

        var result = await _workoutSync.UpdateWorkoutAsync(
            userId, workout.Id, "Title", "2026-03-01", 0, DateTime.UtcNow.AddMinutes(1));

        Assert.Null(result);
    }

    // ── Workout Soft Delete ─────────────────────────────────────────────

    [Fact]
    public async Task SoftDelete_ShouldReturnTrue_WhenWorkoutExists()
    {
        var userId = Guid.NewGuid().ToString();
        var workout = CreateWorkout(userId);
        await _workoutSync.CreateWorkoutAsync(workout);

        var result = await _workoutSync.SoftDeleteWorkoutAsync(userId, workout.Id, DateTime.UtcNow);

        Assert.True(result);

        var userWorkouts = await _workoutRepo.GetByUserIdAsync(userId);
        Assert.DoesNotContain(userWorkouts, w => w.Id == workout.Id);
    }

    [Fact]
    public async Task SoftDelete_ShouldReturnFalse_WhenAlreadyDeleted()
    {
        var userId = Guid.NewGuid().ToString();
        var workout = CreateWorkout(userId);
        await _workoutSync.CreateWorkoutAsync(workout);
        await _workoutSync.SoftDeleteWorkoutAsync(userId, workout.Id, DateTime.UtcNow);

        var result = await _workoutSync.SoftDeleteWorkoutAsync(userId, workout.Id, DateTime.UtcNow);

        Assert.False(result);
    }

    [Fact]
    public async Task SoftDelete_ShouldReturnFalse_WhenWorkoutDoesNotExist()
    {
        var result = await _workoutSync.SoftDeleteWorkoutAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), DateTime.UtcNow);

        Assert.False(result);
    }

    // ── Ownership Validation ──────────────────────────────────────────

    [Fact]
    public async Task UpdateWorkout_ShouldReturnConflict_WhenWrongUser()
    {
        var ownerId = Guid.NewGuid().ToString();
        var workout = CreateWorkout(ownerId);
        await _workoutSync.CreateWorkoutAsync(workout);

        var attackerId = Guid.NewGuid().ToString();
        var conflict = await _workoutSync.UpdateWorkoutAsync(
            attackerId, workout.Id, "Hacked Title", "2026-06-01", 99, DateTime.UtcNow.AddMinutes(1));

        Assert.NotNull(conflict);
        Assert.Equal("Push Day", conflict.Title);
    }

    [Fact]
    public async Task SoftDelete_ShouldReturnFalse_WhenWrongUser()
    {
        var ownerId = Guid.NewGuid().ToString();
        var workout = CreateWorkout(ownerId);
        await _workoutSync.CreateWorkoutAsync(workout);

        var attackerId = Guid.NewGuid().ToString();
        var result = await _workoutSync.SoftDeleteWorkoutAsync(attackerId, workout.Id, DateTime.UtcNow);

        Assert.False(result);

        var fetched = await _workoutRepo.GetByIdAsync(workout.Id);
        Assert.NotNull(fetched);
        Assert.Null(fetched.DeletedAt);
    }
}

[Collection("DynamoDB")]
public class MuscleGroupSyncRepositoryTests(DynamoDbFixture fixture)
{
    private static readonly IOptions<DynamoDbSettings> Settings = Options.Create(new DynamoDbSettings());
    private readonly WorkoutSyncRepository _workoutSync = new(fixture.Client, Settings);
    private readonly MuscleGroupSyncRepository _mgSync = new(fixture.Client, Settings);
    private readonly WorkoutRepository _workoutRepo = new(fixture.Client, Settings);

    private static WorkoutEntity CreateWorkout(string userId) => new()
    {
        Id = Guid.NewGuid().ToString(),
        UserId = userId,
        Title = "Push Day",
        Date = "2026-03-01",
        OrderIndex = 0,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    // ── MuscleGroup Add ─────────────────────────────────────────────────

    [Fact]
    public async Task AddMuscleGroup_ShouldReturnTrue_WhenValidWorkout()
    {
        var userId = Guid.NewGuid().ToString();
        var workout = CreateWorkout(userId);
        await _workoutSync.CreateWorkoutAsync(workout);

        var mgId = Guid.NewGuid().ToString();
        var mg = new MuscleGroupEntity
        {
            Id = mgId, WorkoutId = workout.Id, Title = "Chest", Date = "2026-03-01", OrderIndex = 0
        };

        var result = await _mgSync.AddMuscleGroupAsync(userId, mg, DateTime.UtcNow);

        Assert.True(result);

        var fetched = await _workoutRepo.GetByIdAsync(workout.Id);
        Assert.NotNull(fetched);
        Assert.Equal(mg, fetched.MuscleGroup[mgId], MuscleGroupEntityComparer.Instance);
    }

    [Fact]
    public async Task AddMuscleGroup_ShouldReturnFalse_WhenWorkoutIsDeleted()
    {
        var userId = Guid.NewGuid().ToString();
        var workout = CreateWorkout(userId);
        await _workoutSync.CreateWorkoutAsync(workout);
        await _workoutSync.SoftDeleteWorkoutAsync(userId, workout.Id, DateTime.UtcNow);

        var mg = new MuscleGroupEntity
        {
            Id = Guid.NewGuid().ToString(), WorkoutId = workout.Id, Title = "Chest", Date = "2026-03-01", OrderIndex = 0
        };

        var result = await _mgSync.AddMuscleGroupAsync(userId, mg, DateTime.UtcNow);

        Assert.False(result);
    }

    [Fact]
    public async Task AddMuscleGroup_ShouldReturnFalse_WhenWrongUser()
    {
        var userId = Guid.NewGuid().ToString();
        var workout = CreateWorkout(userId);
        await _workoutSync.CreateWorkoutAsync(workout);

        var mg = new MuscleGroupEntity
        {
            Id = Guid.NewGuid().ToString(), WorkoutId = workout.Id, Title = "Chest", Date = "2026-03-01", OrderIndex = 0
        };

        var result = await _mgSync.AddMuscleGroupAsync("wrong-user", mg, DateTime.UtcNow);

        Assert.False(result);
    }

    // ── MuscleGroup Update ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateMuscleGroup_ShouldReturnFalse_WhenWrongUser()
    {
        var ownerId = Guid.NewGuid().ToString();
        var workout = CreateWorkout(ownerId);
        await _workoutSync.CreateWorkoutAsync(workout);

        var mgId = Guid.NewGuid().ToString();
        var mg = new MuscleGroupEntity
        {
            Id = mgId, WorkoutId = workout.Id, Title = "Chest", Date = "2026-03-01", OrderIndex = 0
        };
        await _mgSync.AddMuscleGroupAsync(ownerId, mg, DateTime.UtcNow);

        var attackerId = Guid.NewGuid().ToString();
        var result = await _mgSync.UpdateMuscleGroupAsync(
            attackerId, workout.Id, mgId, "Hacked", "2026-06-01", 99, DateTime.UtcNow.AddMinutes(1));

        Assert.False(result);

        var fetched = await _workoutRepo.GetByIdAsync(workout.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Chest", fetched.MuscleGroup[mgId].Title);
    }

    [Fact]
    public async Task UpdateMuscleGroup_ShouldUpdateFields_WithoutLosingExercises()
    {
        var userId = Guid.NewGuid().ToString();
        var workout = CreateWorkout(userId);
        await _workoutSync.CreateWorkoutAsync(workout);

        var mgId = Guid.NewGuid().ToString();
        var exId = Guid.NewGuid().ToString();
        var mg = new MuscleGroupEntity
        {
            Id = mgId, WorkoutId = workout.Id, Title = "Chest", Date = "2026-03-01", OrderIndex = 0,
            Exercises = new Dictionary<string, ExerciseEntity>
            {
                [exId] = new ExerciseEntity { Id = exId, MuscleGroupId = mgId, Title = "Bench Press", OrderIndex = 0 }
            }
        };
        await _mgSync.AddMuscleGroupAsync(userId, mg, DateTime.UtcNow);

        var result = await _mgSync.UpdateMuscleGroupAsync(
            userId, workout.Id, mgId, "Updated Chest", "2026-04-01", 1, DateTime.UtcNow.AddMinutes(1));

        Assert.True(result);

        var fetched = await _workoutRepo.GetByIdAsync(workout.Id);
        Assert.NotNull(fetched);
        var expectedMg = new MuscleGroupEntity
        {
            Id = mgId, WorkoutId = workout.Id, Title = "Updated Chest", Date = "2026-04-01", OrderIndex = 1,
            Exercises = mg.Exercises
        };
        Assert.Equal(expectedMg, fetched.MuscleGroup[mgId], MuscleGroupEntityComparer.Instance);
    }

    // ── MuscleGroup Remove ──────────────────────────────────────────────

    [Fact]
    public async Task RemoveMuscleGroup_ShouldRemoveEntry()
    {
        var userId = Guid.NewGuid().ToString();
        var workout = CreateWorkout(userId);
        await _workoutSync.CreateWorkoutAsync(workout);

        var mgId = Guid.NewGuid().ToString();
        var mg = new MuscleGroupEntity
        {
            Id = mgId, WorkoutId = workout.Id, Title = "Chest", Date = "2026-03-01", OrderIndex = 0
        };
        await _mgSync.AddMuscleGroupAsync(userId, mg, DateTime.UtcNow);

        var result = await _mgSync.RemoveMuscleGroupAsync(userId, workout.Id, mgId, DateTime.UtcNow.AddMinutes(1));

        Assert.True(result);

        var fetched = await _workoutRepo.GetByIdAsync(workout.Id);
        Assert.NotNull(fetched);
        Assert.False(fetched.MuscleGroup.ContainsKey(mgId));
    }

    [Fact]
    public async Task RemoveMuscleGroup_ShouldReturnFalse_WhenWrongUser()
    {
        var ownerId = Guid.NewGuid().ToString();
        var workout = CreateWorkout(ownerId);
        await _workoutSync.CreateWorkoutAsync(workout);

        var mgId = Guid.NewGuid().ToString();
        var mg = new MuscleGroupEntity
        {
            Id = mgId, WorkoutId = workout.Id, Title = "Chest", Date = "2026-03-01", OrderIndex = 0
        };
        await _mgSync.AddMuscleGroupAsync(ownerId, mg, DateTime.UtcNow);

        var attackerId = Guid.NewGuid().ToString();
        var result = await _mgSync.RemoveMuscleGroupAsync(attackerId, workout.Id, mgId, DateTime.UtcNow.AddMinutes(1));

        Assert.False(result);

        var fetched = await _workoutRepo.GetByIdAsync(workout.Id);
        Assert.NotNull(fetched);
        Assert.True(fetched.MuscleGroup.ContainsKey(mgId));
    }
}

[Collection("DynamoDB")]
public class ExerciseSyncRepositoryTests(DynamoDbFixture fixture)
{
    private static readonly IOptions<DynamoDbSettings> Settings = Options.Create(new DynamoDbSettings());
    private readonly WorkoutSyncRepository _workoutSync = new(fixture.Client, Settings);
    private readonly MuscleGroupSyncRepository _mgSync = new(fixture.Client, Settings);
    private readonly ExerciseSyncRepository _exSync = new(fixture.Client, Settings);
    private readonly WorkoutRepository _workoutRepo = new(fixture.Client, Settings);

    private static WorkoutEntity CreateWorkout(string userId) => new()
    {
        Id = Guid.NewGuid().ToString(),
        UserId = userId,
        Title = "Push Day",
        Date = "2026-03-01",
        OrderIndex = 0,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private async Task<(WorkoutEntity workout, string mgId)> SetupWithMuscleGroup(string userId)
    {
        var workout = CreateWorkout(userId);
        await _workoutSync.CreateWorkoutAsync(workout);

        var mgId = Guid.NewGuid().ToString();
        var mg = new MuscleGroupEntity
        {
            Id = mgId, WorkoutId = workout.Id, Title = "Chest", Date = "2026-03-01", OrderIndex = 0
        };
        await _mgSync.AddMuscleGroupAsync(userId, mg, DateTime.UtcNow);

        return (workout, mgId);
    }

    // ── Exercise Add ────────────────────────────────────────────────────

    [Fact]
    public async Task AddExercise_ShouldAddToMuscleGroup()
    {
        var userId = Guid.NewGuid().ToString();
        var (workout, mgId) = await SetupWithMuscleGroup(userId);

        var exId = Guid.NewGuid().ToString();
        var exercise = new ExerciseEntity { Id = exId, MuscleGroupId = mgId, Title = "Bench Press", OrderIndex = 0 };

        var result = await _exSync.AddExerciseAsync(workout.Id, userId, exercise, DateTime.UtcNow.AddMinutes(1));

        Assert.True(result);

        var fetched = await _workoutRepo.GetByIdAsync(workout.Id);
        Assert.NotNull(fetched);
        Assert.Equal(exercise, fetched.MuscleGroup[mgId].Exercises[exId], ExerciseEntityComparer.Instance);
    }

    // ── Exercise Update ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateExercise_ShouldReturnFalse_WhenWrongUser()
    {
        var ownerId = Guid.NewGuid().ToString();
        var (workout, mgId) = await SetupWithMuscleGroup(ownerId);

        var exId = Guid.NewGuid().ToString();
        var exercise = new ExerciseEntity { Id = exId, MuscleGroupId = mgId, Title = "Bench Press", OrderIndex = 0 };
        await _exSync.AddExerciseAsync(workout.Id, ownerId, exercise, DateTime.UtcNow);

        var attackerId = Guid.NewGuid().ToString();
        var result = await _exSync.UpdateExerciseAsync(
            attackerId, workout.Id, mgId, exId, "Hacked", 99, DateTime.UtcNow.AddMinutes(1));

        Assert.False(result);

        var fetched = await _workoutRepo.GetByIdAsync(workout.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Bench Press", fetched.MuscleGroup[mgId].Exercises[exId].Title);
    }

    [Fact]
    public async Task UpdateExercise_ShouldUpdateFields_WithoutLosingLogs()
    {
        var userId = Guid.NewGuid().ToString();
        var (workout, mgId) = await SetupWithMuscleGroup(userId);

        var exId = Guid.NewGuid().ToString();
        var logId = Guid.NewGuid().ToString();
        var exercise = new ExerciseEntity
        {
            Id = exId, MuscleGroupId = mgId, Title = "Bench Press", OrderIndex = 0,
            Log = new Dictionary<string, LogEntity>
            {
                [logId] = new LogEntity { Id = logId, NumberReps = 10, MaxWeight = 80, Date = "2026-03-01", OrderIndex = 0 }
            }
        };
        await _exSync.AddExerciseAsync(workout.Id, userId, exercise, DateTime.UtcNow);

        var result = await _exSync.UpdateExerciseAsync(
            userId, workout.Id, mgId, exId, "Incline Press", 1, DateTime.UtcNow.AddMinutes(1));

        Assert.True(result);

        var fetched = await _workoutRepo.GetByIdAsync(workout.Id);
        Assert.NotNull(fetched);
        var expectedEx = new ExerciseEntity
        {
            Id = exId, MuscleGroupId = mgId, Title = "Incline Press", OrderIndex = 1,
            Log = exercise.Log
        };
        Assert.Equal(expectedEx, fetched.MuscleGroup[mgId].Exercises[exId], ExerciseEntityComparer.Instance);
    }

    // ── Exercise Remove ─────────────────────────────────────────────────

    [Fact]
    public async Task RemoveExercise_ShouldRemoveEntry()
    {
        var userId = Guid.NewGuid().ToString();
        var (workout, mgId) = await SetupWithMuscleGroup(userId);

        var exId = Guid.NewGuid().ToString();
        var exercise = new ExerciseEntity { Id = exId, MuscleGroupId = mgId, Title = "Bench Press", OrderIndex = 0 };
        await _exSync.AddExerciseAsync(workout.Id, userId, exercise, DateTime.UtcNow);

        var result = await _exSync.RemoveExerciseAsync(userId, workout.Id, mgId, exId, DateTime.UtcNow.AddMinutes(1));

        Assert.True(result);

        var fetched = await _workoutRepo.GetByIdAsync(workout.Id);
        Assert.NotNull(fetched);
        Assert.False(fetched.MuscleGroup[mgId].Exercises.ContainsKey(exId));
    }

    [Fact]
    public async Task RemoveExercise_ShouldReturnFalse_WhenWrongUser()
    {
        var ownerId = Guid.NewGuid().ToString();
        var (workout, mgId) = await SetupWithMuscleGroup(ownerId);

        var exId = Guid.NewGuid().ToString();
        var exercise = new ExerciseEntity { Id = exId, MuscleGroupId = mgId, Title = "Bench Press", OrderIndex = 0 };
        await _exSync.AddExerciseAsync(workout.Id, ownerId, exercise, DateTime.UtcNow);

        var attackerId = Guid.NewGuid().ToString();
        var result = await _exSync.RemoveExerciseAsync(attackerId, workout.Id, mgId, exId, DateTime.UtcNow.AddMinutes(1));

        Assert.False(result);

        var fetched = await _workoutRepo.GetByIdAsync(workout.Id);
        Assert.NotNull(fetched);
        Assert.True(fetched.MuscleGroup[mgId].Exercises.ContainsKey(exId));
    }
}

[Collection("DynamoDB")]
public class LogSyncRepositoryTests(DynamoDbFixture fixture)
{
    private static readonly IOptions<DynamoDbSettings> Settings = Options.Create(new DynamoDbSettings());
    private readonly WorkoutSyncRepository _workoutSync = new(fixture.Client, Settings);
    private readonly MuscleGroupSyncRepository _mgSync = new(fixture.Client, Settings);
    private readonly ExerciseSyncRepository _exSync = new(fixture.Client, Settings);
    private readonly LogSyncRepository _logSync = new(fixture.Client, Settings);
    private readonly WorkoutRepository _workoutRepo = new(fixture.Client, Settings);

    private static WorkoutEntity CreateWorkout(string userId) => new()
    {
        Id = Guid.NewGuid().ToString(),
        UserId = userId,
        Title = "Push Day",
        Date = "2026-03-01",
        OrderIndex = 0,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private async Task<(WorkoutEntity workout, string mgId, string exId)> SetupWithExercise(string userId)
    {
        var workout = CreateWorkout(userId);
        await _workoutSync.CreateWorkoutAsync(workout);

        var mgId = Guid.NewGuid().ToString();
        var mg = new MuscleGroupEntity
        {
            Id = mgId, WorkoutId = workout.Id, Title = "Chest", Date = "2026-03-01", OrderIndex = 0
        };
        await _mgSync.AddMuscleGroupAsync(userId, mg, DateTime.UtcNow);

        var exId = Guid.NewGuid().ToString();
        var exercise = new ExerciseEntity { Id = exId, MuscleGroupId = mgId, Title = "Bench Press", OrderIndex = 0 };
        await _exSync.AddExerciseAsync(workout.Id, userId, exercise, DateTime.UtcNow);

        return (workout, mgId, exId);
    }

    // ── Log Add ─────────────────────────────────────────────────────────

    [Fact]
    public async Task AddLog_ShouldAddToExercise()
    {
        var userId = Guid.NewGuid().ToString();
        var (workout, mgId, exId) = await SetupWithExercise(userId);

        var logId = Guid.NewGuid().ToString();
        var log = new LogEntity { Id = logId, NumberReps = 10, MaxWeight = 80.5, Date = "2026-03-01", OrderIndex = 0 };

        var result = await _logSync.AddLogAsync(workout.Id, userId, mgId, exId, log, DateTime.UtcNow.AddMinutes(1));

        Assert.True(result);

        var fetched = await _workoutRepo.GetByIdAsync(workout.Id);
        Assert.NotNull(fetched);
        Assert.Equal(log, fetched.MuscleGroup[mgId].Exercises[exId].Log[logId], LogEntityComparer.Instance);
    }

    // ── Log Update ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateLog_ShouldReturnFalse_WhenWrongUser()
    {
        var ownerId = Guid.NewGuid().ToString();
        var (workout, mgId, exId) = await SetupWithExercise(ownerId);

        var logId = Guid.NewGuid().ToString();
        var log = new LogEntity { Id = logId, NumberReps = 10, MaxWeight = 80, Date = "2026-03-01", OrderIndex = 0 };
        await _logSync.AddLogAsync(workout.Id, ownerId, mgId, exId, log, DateTime.UtcNow);

        var attackerId = Guid.NewGuid().ToString();
        var result = await _logSync.UpdateLogAsync(
            attackerId, workout.Id, mgId, exId, logId, 99, 999, DateTime.UtcNow.AddMinutes(1));

        Assert.False(result);

        var fetched = await _workoutRepo.GetByIdAsync(workout.Id);
        Assert.NotNull(fetched);
        Assert.Equal(10, fetched.MuscleGroup[mgId].Exercises[exId].Log[logId].NumberReps);
        Assert.Equal(80, fetched.MuscleGroup[mgId].Exercises[exId].Log[logId].MaxWeight);
    }

    [Fact]
    public async Task UpdateLog_ShouldUpdateFields()
    {
        var userId = Guid.NewGuid().ToString();
        var (workout, mgId, exId) = await SetupWithExercise(userId);

        var logId = Guid.NewGuid().ToString();
        var log = new LogEntity { Id = logId, NumberReps = 10, MaxWeight = 80, Date = "2026-03-01", OrderIndex = 0 };
        await _logSync.AddLogAsync(workout.Id, userId, mgId, exId, log, DateTime.UtcNow);

        var result = await _logSync.UpdateLogAsync(
            userId, workout.Id, mgId, exId, logId, 12, 90.5, DateTime.UtcNow.AddMinutes(1));

        Assert.True(result);

        var fetched = await _workoutRepo.GetByIdAsync(workout.Id);
        Assert.NotNull(fetched);
        var expectedLog = new LogEntity { Id = logId, NumberReps = 12, MaxWeight = 90.5, Date = "2026-03-01", OrderIndex = 0 };
        Assert.Equal(expectedLog, fetched.MuscleGroup[mgId].Exercises[exId].Log[logId], LogEntityComparer.Instance);
    }

    // ── Log Remove ──────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveLog_ShouldRemoveEntry()
    {
        var userId = Guid.NewGuid().ToString();
        var (workout, mgId, exId) = await SetupWithExercise(userId);

        var logId = Guid.NewGuid().ToString();
        var log = new LogEntity { Id = logId, NumberReps = 10, MaxWeight = 80, Date = "2026-03-01", OrderIndex = 0 };
        await _logSync.AddLogAsync(workout.Id, userId, mgId, exId, log, DateTime.UtcNow);

        var result = await _logSync.RemoveLogAsync(
            userId, workout.Id, mgId, exId, logId, DateTime.UtcNow.AddMinutes(1));

        Assert.True(result);

        var fetched = await _workoutRepo.GetByIdAsync(workout.Id);
        Assert.NotNull(fetched);
        Assert.False(fetched.MuscleGroup[mgId].Exercises[exId].Log.ContainsKey(logId));
    }

    [Fact]
    public async Task RemoveLog_ShouldReturnFalse_WhenWrongUser()
    {
        var ownerId = Guid.NewGuid().ToString();
        var (workout, mgId, exId) = await SetupWithExercise(ownerId);

        var logId = Guid.NewGuid().ToString();
        var log = new LogEntity { Id = logId, NumberReps = 10, MaxWeight = 80, Date = "2026-03-01", OrderIndex = 0 };
        await _logSync.AddLogAsync(workout.Id, ownerId, mgId, exId, log, DateTime.UtcNow);

        var attackerId = Guid.NewGuid().ToString();
        var result = await _logSync.RemoveLogAsync(
            attackerId, workout.Id, mgId, exId, logId, DateTime.UtcNow.AddMinutes(1));

        Assert.False(result);

        var fetched = await _workoutRepo.GetByIdAsync(workout.Id);
        Assert.NotNull(fetched);
        Assert.True(fetched.MuscleGroup[mgId].Exercises[exId].Log.ContainsKey(logId));
    }
}
