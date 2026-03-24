using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using replog_api.Auth;
using replog_tests_shared.Fixtures;

namespace replog_api.tests.Fixtures;

public class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestJwtSecret = "CHANGE_THIS_TO_A_SECURE_KEY_AT_LEAST_32_CHARACTERS_LONG";

    private readonly DynamoDbFixture _dynamoDb = new();

    public IGoogleTokenValidator GoogleValidator { get; } = Substitute.For<IGoogleTokenValidator>();

    public async Task InitializeAsync() => await _dynamoDb.InitializeAsync();

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _dynamoDb.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            var dynamo = services.SingleOrDefault(d => d.ServiceType == typeof(IAmazonDynamoDB));
            if (dynamo != null) services.Remove(dynamo);
            services.AddSingleton<IAmazonDynamoDB>(_dynamoDb.Client);

            var gv = services.SingleOrDefault(d => d.ServiceType == typeof(IGoogleTokenValidator));
            if (gv != null) services.Remove(gv);
            services.AddSingleton(GoogleValidator);
        });
    }

    public void SetAuthCookie(HttpClient client, string jwt) =>
        client.DefaultRequestHeaders.Add("Cookie", $"access_token={jwt}");

    public string GenerateJwt(string userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
        var token = new JwtSecurityToken(
            issuer: "replog-api",
            audience: "replog-client",
            claims: [new Claim(ClaimTypes.NameIdentifier, userId)],
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
