using replog_application.Commands;
using replog_application.Interfaces;
using replog_shared.Models.Requests;
using replog_shared.Models.Responses;

namespace replog_api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth")
            .AllowAnonymous();

        group.MapPost("/login", async (
            LoginRequest request,
            ICommandHandler<LoginCommand, AuthResponse> handler) =>
        {
            var command = new LoginCommand { GoogleIdToken = request.GoogleIdToken };
            var result = await handler.HandleAsync(command);
            return Results.Ok(result);
        });

        group.MapPost("/refresh", async (
            RefreshTokenRequest request,
            ICommandHandler<RefreshTokenCommand, AuthResponse> handler) =>
        {
            var command = new RefreshTokenCommand
            {
                AccessToken = request.AccessToken,
                RefreshToken = request.RefreshToken
            };
            var result = await handler.HandleAsync(command);
            return Results.Ok(result);
        });
    }
}
