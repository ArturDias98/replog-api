using replog_tests_shared.Fixtures;

namespace replog_infrastructure.tests.Fixtures;

[CollectionDefinition("DynamoDB")]
public class DynamoDbCollection : ICollectionFixture<DynamoDbFixture>;
