using Microsoft.Extensions.Options;
using replog_api.Auth;
using replog_api.Settings;
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
            IAuthService authService,
            IOptions<JwtSettings> jwtOptions,
            HttpContext context) =>
        {
            var result = await authService.LoginAsync(request.GoogleIdToken, context.RequestAborted);
            if (!result.IsSuccess)
                return Results.Json(
                    new ErrorResponse { Error = result.ErrorCode!, Message = result.ErrorMessage! },
                    statusCode: StatusCodes.Status401Unauthorized);

            var jwt = jwtOptions.Value;
            context.Response.Cookies.Append("access_token", result.Value!.AccessToken, CookieOpts(TimeSpan.FromDays(jwt.AccessTokenCookieExpirationDays)));
            context.Response.Cookies.Append("refresh_token", result.Value!.RefreshToken, CookieOpts(TimeSpan.FromDays(jwt.RefreshTokenCookieExpirationDays)));
            return Results.Ok(new AuthResponse
            {
                ExpiresAt = result.Value!.ExpiresAt,
                UserId = result.Value!.UserId,
                Email = result.Value!.Email,
                DisplayName = result.Value!.DisplayName,
                AvatarUrl = result.Value!.AvatarUrl
            });
        });

        group.MapPost("/refresh", async (
            IAuthService authService,
            IOptions<JwtSettings> jwtOptions,
            HttpContext context) =>
        {
            var accessToken = context.Request.Cookies["access_token"];
            var refreshToken = context.Request.Cookies["refresh_token"];

            if (accessToken is null || refreshToken is null)
                return Results.Json(
                    new ErrorResponse { Error = "missing_tokens", Message = "Auth cookies are missing." },
                    statusCode: StatusCodes.Status401Unauthorized);

            var result = await authService.RefreshTokenAsync(accessToken, refreshToken, context.RequestAborted);
            if (!result.IsSuccess)
                return Results.Json(
                    new ErrorResponse { Error = result.ErrorCode!, Message = result.ErrorMessage! },
                    statusCode: StatusCodes.Status401Unauthorized);

            var jwt = jwtOptions.Value;
            context.Response.Cookies.Append("access_token", result.Value!.AccessToken, CookieOpts(TimeSpan.FromDays(jwt.AccessTokenCookieExpirationDays)));
            context.Response.Cookies.Append("refresh_token", result.Value!.RefreshToken, CookieOpts(TimeSpan.FromDays(jwt.RefreshTokenCookieExpirationDays)));
            return Results.Ok(new AuthResponse
            {
                ExpiresAt = result.Value!.ExpiresAt,
                UserId = result.Value!.UserId,
                Email = result.Value!.Email,
                DisplayName = result.Value!.DisplayName,
                AvatarUrl = result.Value!.AvatarUrl
            });
        });

        group.MapPost("/logout", (HttpContext context) =>
        {
            context.Response.Cookies.Delete("access_token", CookieOpts(TimeSpan.Zero));
            context.Response.Cookies.Delete("refresh_token", CookieOpts(TimeSpan.Zero));
            return Results.Ok();
        });
    }

    private static CookieOptions CookieOpts(TimeSpan maxAge) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.None,
        MaxAge = maxAge,
        Path = "/"
    };
}
