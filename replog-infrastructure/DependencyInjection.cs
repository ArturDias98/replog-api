using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using replog_application.Interfaces;
using replog_application.Interfaces.SyncOperations;
using replog_infrastructure.Repositories;
using replog_infrastructure.Repositories.SyncOperations;
using replog_infrastructure.Settings;

namespace replog_infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DynamoDbSettings>(configuration.GetSection("DynamoDB"));

        services.AddSingleton<IAmazonDynamoDB>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<DynamoDbSettings>>().Value;
            var credentials = new BasicAWSCredentials(settings.AccessKey, settings.SecretKey);
            var clientConfig = new AmazonDynamoDBConfig { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(settings.Region) };

            if (!string.IsNullOrEmpty(settings.ServiceURL))
                clientConfig.ServiceURL = settings.ServiceURL;

            return new AmazonDynamoDBClient(credentials, clientConfig);
        });

        services.AddScoped<IWorkoutRepository, WorkoutRepository>();
        services.AddScoped<IWorkoutSyncRepository, WorkoutSyncRepository>();
        services.AddScoped<IMuscleGroupSyncRepository, MuscleGroupSyncRepository>();
        services.AddScoped<IExerciseSyncRepository, ExerciseSyncRepository>();
        services.AddScoped<ILogSyncRepository, LogSyncRepository>();
        return services;
    }
}
