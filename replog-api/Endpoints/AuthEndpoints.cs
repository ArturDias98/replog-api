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
            HttpContext context,
            IWebHostEnvironment env) =>
        {
            var result = await authService.LoginAsync(request.GoogleIdToken);
            if (!result.IsSuccess)
                return Results.Json(
                    new ErrorResponse { Error = result.ErrorCode!, Message = result.ErrorMessage! },
                    statusCode: StatusCodes.Status401Unauthorized);

            var jwt = jwtOptions.Value;
            context.Response.Cookies.Append("access_token", result.Value!.AccessToken, CookieOpts(env, TimeSpan.FromMinutes(jwt.AccessTokenExpirationMinutes)));
            context.Response.Cookies.Append("refresh_token", result.Value!.RefreshToken, CookieOpts(env, TimeSpan.FromDays(jwt.RefreshTokenExpirationDays)));
            return Results.Ok(new AuthResponse { ExpiresAt = result.Value!.ExpiresAt });
        });

        group.MapPost("/refresh", async (
            IAuthService authService,
            IOptions<JwtSettings> jwtOptions,
            HttpContext context,
            IWebHostEnvironment env) =>
        {
            var accessToken = context.Request.Cookies["access_token"];
            var refreshToken = context.Request.Cookies["refresh_token"];

            if (accessToken is null || refreshToken is null)
                return Results.Json(
                    new ErrorResponse { Error = "missing_tokens", Message = "Auth cookies are missing." },
                    statusCode: StatusCodes.Status401Unauthorized);

            var result = await authService.RefreshTokenAsync(accessToken, refreshToken);
            if (!result.IsSuccess)
                return Results.Json(
                    new ErrorResponse { Error = result.ErrorCode!, Message = result.ErrorMessage! },
                    statusCode: StatusCodes.Status401Unauthorized);

            var jwt = jwtOptions.Value;
            context.Response.Cookies.Append("access_token", result.Value!.AccessToken, CookieOpts(env, TimeSpan.FromMinutes(jwt.AccessTokenExpirationMinutes)));
            context.Response.Cookies.Append("refresh_token", result.Value!.RefreshToken, CookieOpts(env, TimeSpan.FromDays(jwt.RefreshTokenExpirationDays)));
            return Results.Ok(new AuthResponse { ExpiresAt = result.Value!.ExpiresAt });
        });

        group.MapPost("/logout", (HttpContext context, IWebHostEnvironment env) =>
        {
            context.Response.Cookies.Delete("access_token", CookieOpts(env, TimeSpan.Zero));
            context.Response.Cookies.Delete("refresh_token", CookieOpts(env, TimeSpan.Zero));
            return Results.Ok();
        });
    }

    private static CookieOptions CookieOpts(IWebHostEnvironment env, TimeSpan maxAge) => new()
    {
        HttpOnly = true,
        Secure = env.IsProduction(),
        SameSite = SameSiteMode.Lax,
        MaxAge = maxAge,
        Path = "/"
    };
}
