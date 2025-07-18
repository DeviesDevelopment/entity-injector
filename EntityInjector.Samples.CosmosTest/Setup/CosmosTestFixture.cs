using EntityInjector.Samples.CosmosTest.Models;
using Microsoft.Azure.Cosmos;
using Xunit;
using User = EntityInjector.Samples.CosmosTest.Models.User;

namespace EntityInjector.Samples.CosmosTest.Setup;

public class CosmosTestFixture : IAsyncLifetime
{
    private const string AccountKey =
        "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    public CosmosClient Client { get; private set; } = default!;
    public Container UsersContainer { get; private set; } = default!;
    public Container ProductsContainer { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        var connectionString = $"AccountEndpoint=https://localhost:8081/;AccountKey={AccountKey};";
        Client = new CosmosClient(connectionString, new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            HttpClientFactory = () => new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            })
        });

        var db = await Client.CreateDatabaseIfNotExistsAsync("TestDb");

        UsersContainer = await db.Database.CreateContainerIfNotExistsAsync("Users", "/id");
        ProductsContainer = await db.Database.CreateContainerIfNotExistsAsync("Products", "/id");

        await SeedDataAsync();
    }

    public Task DisposeAsync()
    {
        Client?.Dispose();
        return Task.CompletedTask;
    }

    private async Task SeedDataAsync()
    {
        var iterator = UsersContainer.GetItemQueryIterator<dynamic>("SELECT TOP 1 c.id FROM c");
        var response = await iterator.ReadNextAsync();

        if (!response.Resource.Any())
        {
            var user1 = new User { Id = Guid.NewGuid(), Name = "Alice", Age = 20 };
            var user2 = new User { Id = Guid.NewGuid(), Name = "Bob", Age = 18 };
            var user3 = new User { Id = Guid.NewGuid(), Name = "Carol", Age = 25 };

            await UsersContainer.UpsertItemAsync(user1, new PartitionKey(user1.Id.ToString()));
            await UsersContainer.UpsertItemAsync(user2, new PartitionKey(user2.Id.ToString()));
            await UsersContainer.UpsertItemAsync(user3, new PartitionKey(user3.Id.ToString()));
        }

        iterator = ProductsContainer.GetItemQueryIterator<dynamic>("SELECT TOP 1 c.id FROM c");
        response = await iterator.ReadNextAsync();

        if (!response.Resource.Any())
        {
            var product1 = new Product { Id = "1", Name = "Standard Widget", Price = 9.99m };
            var product2 = new Product { Id = "2", Name = "Premium Widget", Price = 19.99m };

            await ProductsContainer.UpsertItemAsync(product1, new PartitionKey(product1.Id));
            await ProductsContainer.UpsertItemAsync(product2, new PartitionKey(product2.Id));
        }
    }
}