using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using replog_tests_shared.Fixtures;

namespace replog_api.tests.Fixtures;

public class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly DynamoDbFixture _dynamoDb = new();

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
        });
    }

    /// <summary>
    /// Simulates the gateway authorizer by setting the trusted x-user-id header
    /// the sync host consumes. Authentication itself is the gateway's job and is
    /// not exercised here.
    /// </summary>
    public void SetUser(HttpClient client, string userId) =>
        client.DefaultRequestHeaders.Add("x-user-id", userId);
}
