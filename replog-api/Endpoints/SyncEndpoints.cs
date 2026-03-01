using replog_api.Auth;
using replog_application.Commands;
using replog_application.Interfaces;
using replog_application.Queries;
using replog_shared.Models.Requests;

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
            ISyncService syncService,
            HttpContext context) =>
        {
            var userId = context.User.GetUserId();
            var command = new PushSyncCommand { UserId = userId, Request = request };
            var result = await syncService.PushAsync(command);
            return Results.Ok(result);
        });

        group.MapGet("/pull", async (ISyncService syncService, HttpContext context) =>
        {
            var userId = context.User.GetUserId();
            var query = new PullSyncQuery { UserId = userId };
            var result = await syncService.PullAsync(query);
            return Results.Ok(result);
        });
    }
}
