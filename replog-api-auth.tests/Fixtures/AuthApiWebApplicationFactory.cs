using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using replog_api_auth.Auth;
using replog_tests_shared.Fixtures;

namespace replog_api_auth.tests.Fixtures;

public class AuthApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
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
}
