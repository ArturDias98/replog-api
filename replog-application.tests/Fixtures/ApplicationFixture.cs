using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;
using replog_application;
using replog_application.Interfaces;
using replog_application.Interfaces.SyncOperations;
using replog_infrastructure.Repositories;
using replog_infrastructure.Repositories.SyncOperations;
using replog_tests_shared.Fixtures;

namespace replog_application.tests.Fixtures;

public class ApplicationFixture : IAsyncLifetime
{
    private readonly DynamoDbFixture _dynamoDb = new();

    public ServiceProvider Provider { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _dynamoDb.InitializeAsync();

        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton<IAmazonDynamoDB>(_dynamoDb.Client);
        services.AddScoped<IWorkoutRepository, WorkoutRepository>();
        services.AddScoped<IWorkoutSyncRepository, WorkoutSyncRepository>();
        services.AddScoped<IMuscleGroupSyncRepository, MuscleGroupSyncRepository>();
        services.AddScoped<IExerciseSyncRepository, ExerciseSyncRepository>();
        services.AddScoped<ILogSyncRepository, LogSyncRepository>();

        Provider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        await Provider.DisposeAsync();
        await _dynamoDb.DisposeAsync();
    }
}
