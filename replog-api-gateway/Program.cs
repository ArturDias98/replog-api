using Microsoft.Extensions.Options;
using replog_api_auth_core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Dev-only: validate the access_token cookie and inject x-user-id, mirroring the
// production API Gateway Lambda authorizer so the sync host's contract is identical.
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton(sp =>
    new AccessTokenValidator(sp.GetRequiredService<IOptions<JwtSettings>>().Value));

var app = builder.Build();

app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    if (path.StartsWithSegments("/api/sync") && !path.StartsWithSegments("/api/sync/health"))
    {
        var validator = context.RequestServices.GetRequiredService<AccessTokenValidator>();
        var token = context.Request.Cookies["access_token"];
        var userId = string.IsNullOrEmpty(token) ? null : validator.GetUserId(token);

        context.Request.Headers.Remove("x-user-id");
        if (!string.IsNullOrEmpty(userId))
            context.Request.Headers["x-user-id"] = userId;
    }

    await next();
});

app.MapReverseProxy();

app.Run();
