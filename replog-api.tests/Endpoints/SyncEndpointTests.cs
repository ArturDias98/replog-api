using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using replog_api.tests.Fixtures;
using replog_shared.Enums;
using replog_shared.Json;
using replog_shared.Models.Requests;
using replog_shared.Models.Responses;

namespace replog_api.tests.Endpoints;

[Collection("Api")]
public class SyncEndpointTests(ApiWebApplicationFactory factory)
{
    [Fact]
    public async Task Pull_ShouldReturn401_WhenNoAuthorizationHeader()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/sync/pull");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Push_ShouldReturn401_WhenNoAuthorizationHeader()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/sync/push",
            new PushSyncRequest { Changes = [] });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Pull_ShouldReturn200WithEmptyWorkouts_WhenAuthorized()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", factory.GenerateJwt(Guid.NewGuid().ToString()));

        var response = await client.GetAsync("/api/sync/pull");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PullSyncResponse>();
        Assert.NotNull(body);
        Assert.Empty(body.Workouts);
    }

    [Fact]
    public async Task Push_ShouldReturn200_WhenAuthorizedWithValidBody()
    {
        var userId = Guid.NewGuid().ToString();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", factory.GenerateJwt(userId));

        var workoutData = JsonSerializer.SerializeToElement(
            new { id = Guid.NewGuid().ToString(), userId, title = "Push Day", date = "2026-03-01", orderIndex = 0 },
            JsonDefaults.Options);

        var request = new PushSyncRequest
        {
            Changes =
            [
                new SyncChangeDto
                {
                    Id = Guid.NewGuid().ToString(),
                    EntityType = EntityType.Workout,
                    Action = ChangeAction.Create,
                    Timestamp = DateTime.UtcNow,
                    Data = workoutData
                }
            ]
        };

        var response = await client.PostAsJsonAsync("/api/sync/push", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PushSyncResponse>();
        Assert.NotNull(body);
        Assert.Single(body.AcknowledgedChangeIds);
    }

    [Fact]
    public async Task Push_ShouldReturn400WithValidationError_WhenChangesListIsEmpty()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", factory.GenerateJwt(Guid.NewGuid().ToString()));

        var response = await client.PostAsJsonAsync("/api/sync/push",
            new PushSyncRequest { Changes = [] });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("validation_error", body.Error);
    }
}
