using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace replog_api_auth_core;

/// <summary>
/// Validates HS256 access tokens issued by the auth Lambda. Shared by the auth
/// host (refresh flow), the API Gateway authorizer, and the dev gateway so the
/// validation parameters live in exactly one place.
/// </summary>
public sealed class AccessTokenValidator(JwtSettings settings)
{
    private readonly JwtSettings _settings = settings;

    /// <summary>
    /// Returns the user id (the <c>sub</c> / <see cref="ClaimTypes.NameIdentifier"/>
    /// claim) when the token's signature, issuer and audience are valid, or
    /// <c>null</c> otherwise. Set <paramref name="validateLifetime"/> to false to
    /// accept expired tokens (used by the refresh flow).
    /// </summary>
    public string? GetUserId(string token, bool validateLifetime = true)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _settings.Issuer,
                ValidateAudience = true,
                ValidAudience = _settings.Audience,
                ValidateLifetime = validateLifetime,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromMinutes(1)
            }, out _);

            return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        catch
        {
            return null;
        }
    }
}
