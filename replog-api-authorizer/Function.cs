using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using replog_api_auth_core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace replog_api_authorizer;

/// <summary>
/// API Gateway HTTP API REQUEST authorizer (simple-response, payload v2).
/// Validates the <c>access_token</c> cookie with the shared HS256 secret and
/// returns the resolved user id as authorizer context. API Gateway maps that
/// id into the <c>x-user-id</c> header consumed by the sync Lambda.
/// </summary>
public class Function
{
    // Resolved once per cold start.
    private static readonly AccessTokenValidator Validator = CreateValidator();

    public APIGatewayCustomAuthorizerV2SimpleResponse FunctionHandler(
        APIGatewayCustomAuthorizerV2Request request,
        ILambdaContext context)
    {
        // CORS preflight — browsers never send credentials on OPTIONS; let API Gateway
        // apply its own CorsConfiguration and return the preflight response.
        if (string.Equals(request.RequestContext?.Http?.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
            return new APIGatewayCustomAuthorizerV2SimpleResponse { IsAuthorized = true };

        var token = ExtractAccessToken(request);
        var userId = token is null ? null : Validator.GetUserId(token);

        if (string.IsNullOrEmpty(userId))
            return new APIGatewayCustomAuthorizerV2SimpleResponse { IsAuthorized = false };

        return new APIGatewayCustomAuthorizerV2SimpleResponse
        {
            IsAuthorized = true,
            Context = new Dictionary<string, object> { ["userId"] = userId }
        };
    }

    private static string? ExtractAccessToken(APIGatewayCustomAuthorizerV2Request request)
    {
        // HTTP API v2 surfaces cookies as a "name=value" array.
        if (request.Cookies is not null)
        {
            foreach (var cookie in request.Cookies)
            {
                var token = MatchAccessToken(cookie);
                if (token is not null) return token;
            }
        }

        // The Cookie header is the configured identity source, so it is always
        // present when the authorizer is invoked — parse it as a fallback.
        if (request.Headers is not null &&
            (request.Headers.TryGetValue("cookie", out var header) ||
             request.Headers.TryGetValue("Cookie", out header)) &&
            !string.IsNullOrEmpty(header))
        {
            foreach (var part in header.Split(';'))
            {
                var token = MatchAccessToken(part);
                if (token is not null) return token;
            }
        }

        return null;
    }

    private static string? MatchAccessToken(string cookie)
    {
        var trimmed = cookie.Trim();
        var sep = trimmed.IndexOf('=');
        if (sep > 0 && trimmed[..sep].Trim() == "access_token")
            return trimmed[(sep + 1)..].Trim();
        return null;
    }

    private static AccessTokenValidator CreateValidator()
    {
        var settings = new JwtSettings
        {
            Secret = ResolveSecret(),
            Issuer = Environment.GetEnvironmentVariable("Jwt__Issuer") ?? "replog-api",
            Audience = Environment.GetEnvironmentVariable("Jwt__Audience") ?? "replog-client"
        };
        return new AccessTokenValidator(settings);
    }

    private static string ResolveSecret()
    {
        // Allow a plain secret via env for local invocation/tests.
        var direct = Environment.GetEnvironmentVariable("Jwt__Secret");
        if (!string.IsNullOrEmpty(direct))
            return direct;

        var arn = Environment.GetEnvironmentVariable("JWT_SECRET_ARN")
            ?? throw new InvalidOperationException("JWT_SECRET_ARN or Jwt__Secret must be set.");

        using var client = new AmazonSecretsManagerClient();
        var response = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = arn })
            .GetAwaiter().GetResult();
        return response.SecretString
            ?? throw new InvalidOperationException($"Secret {arn} has no SecretString value.");
    }
}
