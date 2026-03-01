using System.Security.Claims;

namespace replog_api.Auth;

public static class UserClaimsExtensions
{
    public static string GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? throw new UnauthorizedAccessException("User ID not found in token.");
    }
}
