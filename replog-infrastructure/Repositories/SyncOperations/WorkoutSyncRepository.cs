using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using replog_application.Interfaces.SyncOperations;
using replog_domain.Entities;
using replog_shared.Json;

namespace replog_infrastructure.Repositories.SyncOperations;

public class WorkoutSyncRepository(IAmazonDynamoDB dynamoDbClient)
    : BaseSyncRepository(dynamoDbClient), IWorkoutSyncRepository
{
    public async Task<bool> CreateWorkoutAsync(WorkoutEntity workout)
    {
        var json = JsonSerializer.Serialize(workout, JsonDefaults.Options);
        var doc = Amazon.DynamoDBv2.DocumentModel.Document.FromJson(json);
        var item = doc.ToAttributeMap();

        try
        {
            await DynamoDbClient.PutItemAsync(new PutItemRequest
            {
                TableName = TableName,
                Item = item,
                ConditionExpression = "attribute_not_exists(id)"
            });
            return true;
        }
        catch (ConditionalCheckFailedException)
        {
            return false;
        }
    }

    public async Task<WorkoutEntity?> UpdateWorkoutAsync(
        string workoutId, string title, string date, int orderIndex, DateTime timestamp)
    {
        var current = await GetByIdAsync(workoutId);
        if (current is null || current.DeletedAt is not null)
            return null;

        if (current.UpdatedAt > timestamp)
            return current;

        try
        {
            await DynamoDbClient.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = TableName,
                Key = WorkoutKey(workoutId),
                UpdateExpression = "SET title = :t, #d = :d, orderIndex = :o, updatedAt = :ts",
                ConditionExpression = "updatedAt = :currentTs",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#d"] = "date"
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":t"] = new() { S = title },
                    [":d"] = new() { S = date },
                    [":o"] = new() { N = orderIndex.ToString() },
                    [":ts"] = DateTimeAttr(timestamp),
                    [":currentTs"] = DateTimeAttr(current.UpdatedAt)
                }
            });
            return null;
        }
        catch (ConditionalCheckFailedException)
        {
            return await GetByIdAsync(workoutId);
        }
    }

    public async Task<bool> SoftDeleteWorkoutAsync(string workoutId, DateTime timestamp)
    {
        try
        {
            await DynamoDbClient.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = TableName,
                Key = WorkoutKey(workoutId),
                UpdateExpression = "SET deletedAt = :ts, updatedAt = :ts",
                ConditionExpression = "attribute_exists(id) AND attribute_not_exists(deletedAt)",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":ts"] = DateTimeAttr(timestamp)
                }
            });
            return true;
        }
        catch (ConditionalCheckFailedException)
        {
            return false;
        }
    }
}
