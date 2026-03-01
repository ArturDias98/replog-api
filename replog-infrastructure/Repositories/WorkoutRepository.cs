using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using replog_application.Interfaces;
using replog_shared.Json;
using replog_shared.Models.Entities;

namespace replog_infrastructure.Repositories;

public class WorkoutRepository(IAmazonDynamoDB dynamoDbClient) : IWorkoutRepository
{
    private const string TableName = "replog-workouts";
    private const string UserIdIndexName = "UserIdIndex";

    public async Task<WorkoutEntity?> GetByIdAsync(string workoutId)
    {
        var response = await dynamoDbClient.GetItemAsync(new GetItemRequest
        {
            TableName = TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = workoutId }
            }
        });

        return response.Item.Count == 0 ? null : MapToEntity(response.Item);
    }

    public async Task<List<WorkoutEntity>> GetByUserIdAsync(string userId)
    {
        var results = new List<WorkoutEntity>();
        Dictionary<string, AttributeValue>? lastEvaluatedKey = null;

        do
        {
            var response = await dynamoDbClient.QueryAsync(new QueryRequest
            {
                TableName = TableName,
                IndexName = UserIdIndexName,
                KeyConditionExpression = "userId = :uid",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":uid"] = new() { S = userId }
                },
                ExclusiveStartKey = lastEvaluatedKey
            });

            foreach (var item in response.Items)
            {
                var entity = MapToEntity(item);
                if (entity.DeletedAt == null)
                    results.Add(entity);
            }

            lastEvaluatedKey = response.LastEvaluatedKey.Count > 0 ? response.LastEvaluatedKey : null;
        } while (lastEvaluatedKey != null);

        return results;
    }

    public async Task PutAsync(WorkoutEntity workout)
    {
        var json = JsonSerializer.Serialize(workout, JsonDefaults.Options);
        var doc = Amazon.DynamoDBv2.DocumentModel.Document.FromJson(json);
        var item = doc.ToAttributeMap();

        await dynamoDbClient.PutItemAsync(new PutItemRequest
        {
            TableName = TableName,
            Item = item
        });
    }

    private static WorkoutEntity MapToEntity(Dictionary<string, AttributeValue> item)
    {
        var doc = Amazon.DynamoDBv2.DocumentModel.Document.FromAttributeMap(item);
        var json = doc.ToJson();
        return JsonSerializer.Deserialize<WorkoutEntity>(json, JsonDefaults.Options)
               ?? throw new InvalidOperationException("Failed to deserialize workout from DynamoDB item.");
    }
}
