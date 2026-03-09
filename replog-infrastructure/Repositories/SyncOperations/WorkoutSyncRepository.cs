using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using replog_application.Interfaces.SyncOperations;
using replog_domain.Entities;
using replog_infrastructure.Settings;
using replog_shared.Json;

namespace replog_infrastructure.Repositories.SyncOperations;

public class WorkoutSyncRepository(IAmazonDynamoDB dynamoDbClient, IOptions<DynamoDbSettings> settings)
    : BaseSyncRepository(dynamoDbClient, settings), IWorkoutSyncRepository
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
        string userId, string workoutId, string title, string date, int orderIndex, DateTime timestamp)
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
                ConditionExpression = "updatedAt = :currentTs AND userId = :uid",
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
                    [":currentTs"] = DateTimeAttr(current.UpdatedAt),
                    [":uid"] = new() { S = userId }
                }
            });
            return null;
        }
        catch (ConditionalCheckFailedException)
        {
            return await GetByIdAsync(workoutId);
        }
    }

    public async Task<bool> SoftDeleteWorkoutAsync(string userId, string workoutId, DateTime timestamp)
    {
        try
        {
            await DynamoDbClient.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = TableName,
                Key = WorkoutKey(workoutId),
                UpdateExpression = "SET deletedAt = :ts, updatedAt = :ts",
                ConditionExpression = "attribute_exists(id) AND attribute_not_exists(deletedAt) AND userId = :uid",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":ts"] = DateTimeAttr(timestamp),
                    [":uid"] = new() { S = userId }
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
