using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace replog_tests_shared.Fixtures;

public class DynamoDbFixture : IAsyncLifetime
{
    private const string TableName = "replog-workouts";
    private const string UserIdIndexName = "UserIdIndex";

    private readonly IContainer _container = new ContainerBuilder()
        .WithImage("amazon/dynamodb-local:latest")
        .WithPortBinding(8000, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8000))
        .Build();

    public IAmazonDynamoDB Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var port = _container.GetMappedPublicPort(8000);

        Client = new AmazonDynamoDBClient(
            new Amazon.Runtime.BasicAWSCredentials("test", "test"),
            new AmazonDynamoDBConfig
            {
                ServiceURL = $"http://localhost:{port}"
            });

        await Client.CreateTableAsync(new CreateTableRequest
        {
            TableName = TableName,
            KeySchema =
            [
                new KeySchemaElement("id", KeyType.HASH)
            ],
            AttributeDefinitions =
            [
                new AttributeDefinition("id", ScalarAttributeType.S),
                new AttributeDefinition("userId", ScalarAttributeType.S)
            ],
            GlobalSecondaryIndexes =
            [
                new GlobalSecondaryIndex
                {
                    IndexName = UserIdIndexName,
                    KeySchema = [new KeySchemaElement("userId", KeyType.HASH)],
                    Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    ProvisionedThroughput = new ProvisionedThroughput(5, 5)
                }
            ],
            ProvisionedThroughput = new ProvisionedThroughput(5, 5)
        });
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await _container.DisposeAsync();
    }
}
