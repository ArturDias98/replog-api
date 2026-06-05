using System.Security.Claims;

namespace replog_api.Middleware;

/// <summary>
/// Trusts the <c>x-user-id</c> header injected by the upstream ingress
/// (the API Gateway Lambda authorizer in production, the YARP gateway in dev)
/// and turns it into the request's <see cref="ClaimsPrincipal"/>. The sync host
/// performs no token validation of its own — authentication happens at the
/// gateway. Requests to <c>/api/sync/*</c> without the header are rejected 401;
/// <c>/api/sync/health</c> is public.
/// </summary>
public class TrustedUserMiddleware(RequestDelegate next)
{
    private const string UserIdHeader = "x-user-id";

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;
        var isProtectedSync =
            path.StartsWithSegments("/api/sync") &&
            !path.StartsWithSegments("/api/sync/health");

        if (isProtectedSync)
        {
            var userId = context.Request.Headers[UserIdHeader].ToString();
            if (string.IsNullOrEmpty(userId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "Gateway");
            context.User = new ClaimsPrincipal(identity);
        }

        await next(context);
    }
}
