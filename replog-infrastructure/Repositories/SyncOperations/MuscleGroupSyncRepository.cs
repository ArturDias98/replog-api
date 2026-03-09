using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using replog_application.Interfaces.SyncOperations;
using replog_domain.Entities;
using replog_infrastructure.Settings;

namespace replog_infrastructure.Repositories.SyncOperations;

public class MuscleGroupSyncRepository(IAmazonDynamoDB dynamoDbClient, IOptions<DynamoDbSettings> settings)
    : BaseSyncRepository(dynamoDbClient, settings), IMuscleGroupSyncRepository
{
    public async Task<bool> AddMuscleGroupAsync(
        string userId, MuscleGroupEntity muscleGroup, DateTime timestamp)
    {
        try
        {
            await DynamoDbClient.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = TableName,
                Key = WorkoutKey(muscleGroup.WorkoutId),
                UpdateExpression = "SET muscleGroup.#mgId = :val, updatedAt = :ts",
                ConditionExpression = "attribute_exists(id) AND attribute_not_exists(deletedAt) AND userId = :uid AND attribute_not_exists(muscleGroup.#mgId)",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#mgId"] = muscleGroup.Id
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":val"] = SerializeEntity(muscleGroup),
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

    public async Task<bool> UpdateMuscleGroupAsync(
        string userId, string workoutId, string mgId, string title, string date, int orderIndex, DateTime timestamp)
    {
        try
        {
            await DynamoDbClient.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = TableName,
                Key = WorkoutKey(workoutId),
                UpdateExpression = "SET muscleGroup.#mgId.title = :t, muscleGroup.#mgId.#d = :d, muscleGroup.#mgId.orderIndex = :o, updatedAt = :ts",
                ConditionExpression = "attribute_exists(id) AND attribute_not_exists(deletedAt) AND userId = :uid AND attribute_exists(muscleGroup.#mgId)",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#mgId"] = mgId,
                    ["#d"] = "date"
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":t"] = new() { S = title },
                    [":d"] = new() { S = date },
                    [":o"] = new() { N = orderIndex.ToString() },
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

    public async Task<bool> RemoveMuscleGroupAsync(string userId, string workoutId, string mgId, DateTime timestamp)
    {
        try
        {
            await DynamoDbClient.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = TableName,
                Key = WorkoutKey(workoutId),
                UpdateExpression = "REMOVE muscleGroup.#mgId SET updatedAt = :ts",
                ConditionExpression = "attribute_exists(id) AND attribute_not_exists(deletedAt) AND userId = :uid AND attribute_exists(muscleGroup.#mgId)",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#mgId"] = mgId
                },
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
