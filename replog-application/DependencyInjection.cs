using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using replog_application.Commands;
using replog_application.Commands.Handlers;
using replog_application.Commands.Handlers.Processors;
using replog_application.Interfaces;
using replog_application.Queries;
using replog_application.Queries.Handlers;
using replog_application.Validators;
using replog_shared.Models.Responses;

namespace replog_application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<PushSyncRequestValidator>();

        // CQRS handlers
        services.AddScoped<ICommandHandler<PushSyncCommand, Result<PushSyncResponse>>, PushSyncCommandHandler>();
        services.AddScoped<IQueryHandler<PullSyncQuery, PullSyncResponse>, PullSyncQueryHandler>();

        // Change processors
        services.AddScoped<IChangeProcessor, WorkoutChangeProcessor>();
        services.AddScoped<IChangeProcessor, MuscleGroupChangeProcessor>();
        services.AddScoped<IChangeProcessor, ExerciseChangeProcessor>();
        services.AddScoped<IChangeProcessor, LogChangeProcessor>();

        return services;
    }
}
