using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using replog_application.Interfaces.SyncOperations;
using replog_domain.Entities;

namespace replog_infrastructure.Repositories.SyncOperations;

public class ExerciseSyncRepository(IAmazonDynamoDB dynamoDbClient)
    : BaseSyncRepository(dynamoDbClient), IExerciseSyncRepository
{
    public async Task<bool> AddExerciseAsync(
        string workoutId, string userId, ExerciseEntity exercise, DateTime timestamp)
    {
        try
        {
            await DynamoDbClient.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = TableName,
                Key = WorkoutKey(workoutId),
                UpdateExpression = "SET muscleGroup.#mgId.exercises.#exId = :val, updatedAt = :ts",
                ConditionExpression = "attribute_exists(id) AND attribute_not_exists(deletedAt) AND userId = :uid AND attribute_exists(muscleGroup.#mgId) AND attribute_not_exists(muscleGroup.#mgId.exercises.#exId)",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#mgId"] = exercise.MuscleGroupId,
                    ["#exId"] = exercise.Id
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":val"] = SerializeEntity(exercise),
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

    public async Task<bool> UpdateExerciseAsync(
        string userId, string workoutId, string mgId, string exId, string title, int orderIndex, DateTime timestamp)
    {
        try
        {
            await DynamoDbClient.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = TableName,
                Key = WorkoutKey(workoutId),
                UpdateExpression = "SET muscleGroup.#mgId.exercises.#exId.title = :t, muscleGroup.#mgId.exercises.#exId.orderIndex = :o, updatedAt = :ts",
                ConditionExpression = "attribute_exists(id) AND attribute_not_exists(deletedAt) AND userId = :uid AND attribute_exists(muscleGroup.#mgId.exercises.#exId)",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#mgId"] = mgId,
                    ["#exId"] = exId
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":t"] = new() { S = title },
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

    public async Task<bool> RemoveExerciseAsync(string userId, string workoutId, string mgId, string exId, DateTime timestamp)
    {
        try
        {
            await DynamoDbClient.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = TableName,
                Key = WorkoutKey(workoutId),
                UpdateExpression = "REMOVE muscleGroup.#mgId.exercises.#exId SET updatedAt = :ts",
                ConditionExpression = "attribute_exists(id) AND attribute_not_exists(deletedAt) AND userId = :uid AND attribute_exists(muscleGroup.#mgId.exercises.#exId)",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#mgId"] = mgId,
                    ["#exId"] = exId
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
