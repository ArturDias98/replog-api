using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace replog_api_host;

public static class SecretsLoader
{
    public static async Task LoadFromSecretsManagerAsync(
        WebApplicationBuilder builder,
        CancellationToken cancellationToken = default)
    {
        if (!builder.Environment.IsProduction())
            return;

        var jwtSecretArn = Environment.GetEnvironmentVariable("JWT_SECRET_ARN")
            ?? throw new InvalidOperationException("JWT_SECRET_ARN env var is required in Production.");

        using var client = new AmazonSecretsManagerClient();
        var jwtSecret = await GetSecretStringAsync(client, jwtSecretArn, cancellationToken);

        var settings = new Dictionary<string, string?> { ["Jwt:Secret"] = jwtSecret };

        var googleClientIdArn = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID_ARN");
        if (!string.IsNullOrEmpty(googleClientIdArn))
        {
            var googleClientId = await GetSecretStringAsync(client, googleClientIdArn, cancellationToken);
            settings["Google:ClientId"] = googleClientId;
        }

        builder.Configuration.AddInMemoryCollection(settings);
    }

    private static async Task<string> GetSecretStringAsync(
        IAmazonSecretsManager client, string secretId, CancellationToken cancellationToken)
    {
        var response = await client.GetSecretValueAsync(
            new GetSecretValueRequest { SecretId = secretId },
            cancellationToken);
        return response.SecretString
            ?? throw new InvalidOperationException($"Secret {secretId} has no SecretString value.");
    }
}
