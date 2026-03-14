using replog_api.Auth;
using replog_application;
using replog_application.Commands;
using replog_application.Interfaces;
using replog_application.Queries;
using replog_shared.Models.Requests;
using replog_shared.Models.Responses;

namespace replog_api.Endpoints;

public static class SyncEndpoints
{
    public static void MapSyncEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/sync")
            .RequireAuthorization()
            .RequireRateLimiting("sync");

        group.MapPost("/push", async (
            PushSyncRequest request,
            ICommandHandler<PushSyncCommand, Result<PushSyncResponse>> handler,
            HttpContext context) =>
        {
            var userId = context.User.GetUserId();
            var command = new PushSyncCommand { UserId = userId, Request = request };
            var result = await handler.HandleAsync(command);
            if (!result.IsSuccess)
                return Results.Json(
                    new ErrorResponse { Error = result.ErrorCode!, Message = result.ErrorMessage! },
                    statusCode: StatusCodes.Status400BadRequest);
            return Results.Ok(result.Value);
        });

        group.MapGet("/pull", async (
            IQueryHandler<PullSyncQuery, PullSyncResponse> handler,
            HttpContext context) =>
        {
            var userId = context.User.GetUserId();
            var query = new PullSyncQuery { UserId = userId };
            return Results.Ok(await handler.HandleAsync(query));
        });
    }
}
