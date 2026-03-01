using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;
using replog_application.Interfaces;
using replog_infrastructure.Repositories;

namespace replog_infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddAWSService<IAmazonDynamoDB>();
        services.AddScoped<IWorkoutRepository, WorkoutRepository>();
        return services;
    }
}
