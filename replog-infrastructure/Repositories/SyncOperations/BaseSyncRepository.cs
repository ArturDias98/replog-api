using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using replog_domain.Entities;
using replog_infrastructure.Settings;
using replog_shared.Json;

namespace replog_infrastructure.Repositories.SyncOperations;

public abstract class BaseSyncRepository(IAmazonDynamoDB dynamoDbClient, IOptions<DynamoDbSettings> settings)
{
    protected readonly string TableName = settings.Value.TableName;
    protected readonly IAmazonDynamoDB DynamoDbClient = dynamoDbClient;

    protected static Dictionary<string, AttributeValue> WorkoutKey(string workoutId) =>
        new() { ["id"] = new() { S = workoutId } };

    protected static AttributeValue DateTimeAttr(DateTime value)
    {
        var str = JsonSerializer.Serialize(value).Trim('"');
        return new AttributeValue { S = str };
    }

    protected static AttributeValue SerializeEntity<T>(T entity)
    {
        var json = JsonSerializer.Serialize(entity, JsonDefaults.Options);
        var doc = Amazon.DynamoDBv2.DocumentModel.Document.FromJson(json);
        return new AttributeValue { M = doc.ToAttributeMap() };
    }

    protected async Task<WorkoutEntity?> GetByIdAsync(string workoutId, CancellationToken cancellationToken = default)
    {
        var response = await DynamoDbClient.GetItemAsync(new GetItemRequest
        {
            TableName = TableName,
            Key = WorkoutKey(workoutId)
        }, cancellationToken);

        if (response.Item is null || response.Item.Count == 0)
            return null;

        var doc = Amazon.DynamoDBv2.DocumentModel.Document.FromAttributeMap(response.Item);
        var json = doc.ToJson();
        return JsonSerializer.Deserialize<WorkoutEntity>(json, JsonDefaults.Options);
    }
}
