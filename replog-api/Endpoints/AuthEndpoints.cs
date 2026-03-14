using replog_api.Auth;
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
            IAuthService authService) =>
        {
            var result = await authService.LoginAsync(request.GoogleIdToken);
            if (!result.IsSuccess)
                return Results.Json(
                    new ErrorResponse { Error = result.ErrorCode!, Message = result.ErrorMessage! },
                    statusCode: StatusCodes.Status401Unauthorized);
            return Results.Ok(result.Value);
        });

        group.MapPost("/refresh", async (
            RefreshTokenRequest request,
            IAuthService authService) =>
        {
            var result = await authService.RefreshTokenAsync(request.AccessToken, request.RefreshToken);
            if (!result.IsSuccess)
                return Results.Json(
                    new ErrorResponse { Error = result.ErrorCode!, Message = result.ErrorMessage! },
                    statusCode: StatusCodes.Status401Unauthorized);
            return Results.Ok(result.Value);
        });
    }
}
