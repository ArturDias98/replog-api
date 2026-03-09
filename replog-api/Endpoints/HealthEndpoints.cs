using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using replog_infrastructure.Settings;

namespace replog_api.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/api/health", async (IAmazonDynamoDB dynamoDb, IOptions<DynamoDbSettings> settings) =>
        {
            try
            {
                await dynamoDb.DescribeTableAsync(new DescribeTableRequest
                {
                    TableName = settings.Value.TableName
                });

                return Results.Ok(new { status = "healthy", database = "connected" });
            }
            catch
            {
                return Results.Json(
                    new { status = "unhealthy", database = "disconnected" },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }).AllowAnonymous();
    }
}
