using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
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
        services.AddSingleton<IAmazonDynamoDB>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var serviceUrl = config["AWS:ServiceURL"];

            if (!string.IsNullOrEmpty(serviceUrl))
            {
                var clientConfig = new AmazonDynamoDBConfig { ServiceURL = serviceUrl };
                return new AmazonDynamoDBClient(new BasicAWSCredentials("local", "local"), clientConfig);
            }

            return new AmazonDynamoDBClient();
        });
        services.AddScoped<IWorkoutRepository, WorkoutRepository>();
        services.AddScoped<IWorkoutSyncRepository, WorkoutSyncRepository>();
        services.AddScoped<IMuscleGroupSyncRepository, MuscleGroupSyncRepository>();
        services.AddScoped<IExerciseSyncRepository, ExerciseSyncRepository>();
        services.AddScoped<ILogSyncRepository, LogSyncRepository>();
        return services;
    }
}
