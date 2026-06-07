using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using replog_infrastructure.Settings;

namespace replog_api.Extensions;

public static class HealthEndpointExtensions
{
    public static IEndpointRouteBuilder MapHealthEndpoint(this IEndpointRouteBuilder endpoints, string path)
    {
        endpoints.MapGet(path, async (IAmazonDynamoDB dynamoDb, IOptions<DynamoDbSettings> settings, HttpContext context) =>
        {
            try
            {
                await dynamoDb.DescribeTableAsync(new DescribeTableRequest
                {
                    TableName = settings.Value.TableName
                }, context.RequestAborted);

                return Results.Ok(new { status = "healthy", database = "connected" });
            }
            catch
            {
                return Results.Json(
                    new { status = "unhealthy", database = "disconnected" },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }).AllowAnonymous();

        return endpoints;
    }
}
