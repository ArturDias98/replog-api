using System.Net;
using System.Text.Json;
using replog_api.tests.Fixtures;

namespace replog_api.tests.Endpoints;

[Collection("Api")]
public class HealthEndpointTests(ApiWebApplicationFactory factory)
{
    [Fact]
    public async Task Health_ShouldReturn200WithHealthyStatus_WhenDynamoDbIsAvailable()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("healthy", doc.RootElement.GetProperty("status").GetString());
    }
}
