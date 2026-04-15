using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using replog_application.Interfaces;
using replog_domain.Entities;
using replog_infrastructure.Settings;
using replog_shared.Json;

namespace replog_infrastructure.Repositories;

public class UserRepository(IAmazonDynamoDB dynamoDbClient, IOptions<DynamoDbSettings> settings) : IUserRepository
{
    private readonly string _tableName = settings.Value.UsersTableName;

    public async Task<UserEntity?> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var response = await dynamoDbClient.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = userId }
            }
        }, cancellationToken);

        return response.Item is null || response.Item.Count == 0 ? null : MapToEntity(response.Item);
    }

    public async Task UpsertAsync(UserEntity user, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(user, JsonDefaults.Options);
        var doc = Amazon.DynamoDBv2.DocumentModel.Document.FromJson(json);
        var item = doc.ToAttributeMap();

        await dynamoDbClient.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = item
        }, cancellationToken);
    }

    public async Task AddRefreshTokenAsync(string userId, RefreshTokenEntry entry, CancellationToken cancellationToken = default)
    {
        var entryMap = new AttributeValue
        {
            M = new Dictionary<string, AttributeValue>
            {
                ["tokenHash"] = new() { S = entry.TokenHash },
                ["expiresAt"] = new() { S = entry.ExpiresAt.ToString("o") }
            }
        };

        await dynamoDbClient.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = userId }
            },
            UpdateExpression = "SET refreshTokens = list_append(if_not_exists(refreshTokens, :empty), :newToken), updatedAt = :now",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":newToken"] = new() { L = [entryMap] },
                [":empty"] = new() { L = [] },
                [":now"] = new() { S = DateTime.UtcNow.ToString("o") }
            },
            ConditionExpression = "attribute_exists(id)"
        }, cancellationToken);
    }

    public async Task ReplaceRefreshTokenAsync(string userId, string oldTokenHash, RefreshTokenEntry newEntry, CancellationToken cancellationToken = default)
    {
        var user = await GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        var index = user.RefreshTokens.FindIndex(rt => rt.TokenHash == oldTokenHash);
        if (index < 0)
            throw new InvalidOperationException("Refresh token not found.");

        user.RefreshTokens[index] = newEntry;

        // Remove expired tokens while we're at it
        var now = DateTime.UtcNow;
        user.RefreshTokens.RemoveAll(rt => rt.ExpiresAt < now && rt.TokenHash != newEntry.TokenHash);
        user.UpdatedAt = now;

        await UpsertAsync(user, cancellationToken);
    }

    private static UserEntity MapToEntity(Dictionary<string, AttributeValue> item)
    {
        var doc = Amazon.DynamoDBv2.DocumentModel.Document.FromAttributeMap(item);
        var json = doc.ToJson();
        return JsonSerializer.Deserialize<UserEntity>(json, JsonDefaults.Options)
               ?? throw new InvalidOperationException("Failed to deserialize user from DynamoDB item.");
    }
}
