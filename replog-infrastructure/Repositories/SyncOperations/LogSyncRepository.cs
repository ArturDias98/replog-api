using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using replog_application.Interfaces.SyncOperations;
using replog_domain.Entities;
using replog_infrastructure.Settings;

namespace replog_infrastructure.Repositories.SyncOperations;

public class LogSyncRepository(IAmazonDynamoDB dynamoDbClient, IOptions<DynamoDbSettings> settings)
    : BaseSyncRepository(dynamoDbClient, settings), ILogSyncRepository
{
    public async Task<bool> AddLogAsync(
        string workoutId, string userId, string mgId, string exId, LogEntity log, DateTime timestamp)
    {
        try
        {
            await DynamoDbClient.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = TableName,
                Key = WorkoutKey(workoutId),
                UpdateExpression = "SET muscleGroup.#mgId.exercises.#exId.#log.#logId = :val, updatedAt = :ts",
                ConditionExpression = "attribute_exists(id) AND attribute_not_exists(deletedAt) AND userId = :uid AND attribute_exists(muscleGroup.#mgId.exercises.#exId) AND attribute_not_exists(muscleGroup.#mgId.exercises.#exId.#log.#logId)",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#mgId"] = mgId,
                    ["#exId"] = exId,
                    ["#log"] = "log",
                    ["#logId"] = log.Id
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":val"] = SerializeEntity(log),
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

    public async Task<bool> UpdateLogAsync(
        string userId, string workoutId, string mgId, string exId, string logId,
        int numberReps, double maxWeight, DateTime timestamp)
    {
        try
        {
            await DynamoDbClient.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = TableName,
                Key = WorkoutKey(workoutId),
                UpdateExpression = "SET muscleGroup.#mgId.exercises.#exId.#log.#logId.numberReps = :nr, muscleGroup.#mgId.exercises.#exId.#log.#logId.maxWeight = :mw, updatedAt = :ts",
                ConditionExpression = "attribute_exists(id) AND attribute_not_exists(deletedAt) AND userId = :uid AND attribute_exists(muscleGroup.#mgId.exercises.#exId.#log.#logId)",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#mgId"] = mgId,
                    ["#exId"] = exId,
                    ["#log"] = "log",
                    ["#logId"] = logId
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":nr"] = new() { N = numberReps.ToString() },
                    [":mw"] = new() { N = maxWeight.ToString(CultureInfo.InvariantCulture) },
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

    public async Task<bool> RemoveLogAsync(
        string userId, string workoutId, string mgId, string exId, string logId, DateTime timestamp)
    {
        try
        {
            await DynamoDbClient.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = TableName,
                Key = WorkoutKey(workoutId),
                UpdateExpression = "REMOVE muscleGroup.#mgId.exercises.#exId.#log.#logId SET updatedAt = :ts",
                ConditionExpression = "attribute_exists(id) AND attribute_not_exists(deletedAt) AND userId = :uid AND attribute_exists(muscleGroup.#mgId.exercises.#exId.#log.#logId)",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#mgId"] = mgId,
                    ["#exId"] = exId,
                    ["#log"] = "log",
                    ["#logId"] = logId
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
