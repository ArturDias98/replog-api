using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;
using replog_application.Interfaces;
using replog_application.Interfaces.SyncOperations;
using replog_infrastructure.Repositories;
using replog_infrastructure.Repositories.SyncOperations;

namespace replog_infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddAWSService<IAmazonDynamoDB>();
        services.AddScoped<IWorkoutRepository, WorkoutRepository>();
        services.AddScoped<IWorkoutSyncRepository, WorkoutSyncRepository>();
        services.AddScoped<IMuscleGroupSyncRepository, MuscleGroupSyncRepository>();
        services.AddScoped<IExerciseSyncRepository, ExerciseSyncRepository>();
        services.AddScoped<ILogSyncRepository, LogSyncRepository>();
        return services;
    }
}
