using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using replog_application.Interfaces;
using replog_domain.Entities;
using replog_infrastructure.Settings;
using replog_shared.Json;

namespace replog_infrastructure.Repositories;

public class WorkoutRepository(IAmazonDynamoDB dynamoDbClient, IOptions<DynamoDbSettings> settings) : IWorkoutRepository
{
    private readonly string _tableName = settings.Value.TableName;
    private const string UserIdIndexName = "UserIdIndex";

    public async Task<WorkoutEntity?> GetByIdAsync(string workoutId, CancellationToken cancellationToken = default)
    {
        var response = await dynamoDbClient.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = workoutId }
            }
        }, cancellationToken);

        return response.Item is null || response.Item.Count == 0 ? null : MapToEntity(response.Item);
    }

    public async Task<List<WorkoutEntity>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var results = new List<WorkoutEntity>();
        Dictionary<string, AttributeValue>? lastEvaluatedKey = null;

        do
        {
            var response = await dynamoDbClient.QueryAsync(new QueryRequest
            {
                TableName = _tableName,
                IndexName = UserIdIndexName,
                KeyConditionExpression = "userId = :uid",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":uid"] = new() { S = userId }
                },
                ExclusiveStartKey = lastEvaluatedKey
            }, cancellationToken);

            foreach (var item in response.Items)
            {
                var entity = MapToEntity(item);
                if (entity.DeletedAt == null)
                    results.Add(entity);
            }

            lastEvaluatedKey = response.LastEvaluatedKey is { Count: > 0 } ? response.LastEvaluatedKey : null;
        } while (lastEvaluatedKey != null);

        return results;
    }

    public async Task PutAsync(WorkoutEntity workout, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(workout, JsonDefaults.Options);
        var doc = Amazon.DynamoDBv2.DocumentModel.Document.FromJson(json);
        var item = doc.ToAttributeMap();

        await dynamoDbClient.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = item
        }, cancellationToken);
    }

    private static WorkoutEntity MapToEntity(Dictionary<string, AttributeValue> item)
    {
        var doc = Amazon.DynamoDBv2.DocumentModel.Document.FromAttributeMap(item);
        var json = doc.ToJson();
        return JsonSerializer.Deserialize<WorkoutEntity>(json, JsonDefaults.Options)
               ?? throw new InvalidOperationException("Failed to deserialize workout from DynamoDB item.");
    }
}
