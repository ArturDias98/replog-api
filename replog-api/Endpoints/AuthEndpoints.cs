using replog_api.Auth;
using replog_shared.Models.Requests;

namespace replog_api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth")
            .AllowAnonymous();

        group.MapPost("/login", async (
            LoginRequest request,
            IAuthService authService) =>
        {
            var result = await authService.LoginAsync(request.GoogleIdToken);
            return Results.Ok(result);
        });

        group.MapPost("/refresh", async (
            RefreshTokenRequest request,
            IAuthService authService) =>
        {
            var result = await authService.RefreshTokenAsync(request.AccessToken, request.RefreshToken);
            return Results.Ok(result);
        });
    }
}
