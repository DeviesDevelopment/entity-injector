using System.Text.Json;
using EntityInjector.Route.Interfaces;
using EntityInjector.Route.Middleware.BindingMetadata.Collection;
using EntityInjector.Route.Middleware.BindingMetadata.Entity;
using EntityInjector.Samples.CosmosTest.DataReceivers;
using EntityInjector.Samples.CosmosTest.Models;
using EntityInjector.Samples.CosmosTest.Setup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.DependencyInjection;
using xRetry;
using Xunit;

namespace EntityInjector.Samples.CosmosTest.Tests;

public class StringKeyTests : IClassFixture<CosmosTestFixture>
{
    private readonly HttpClient _client;
    private readonly CosmosTestFixture _fixture;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    public StringKeyTests(CosmosTestFixture fixture)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(fixture.Client);
                services.AddSingleton(new CosmosContainer<User>(fixture.UsersContainer));

                services.AddScoped<IBindingModelDataReceiver<string, User>, StringUserDataReceiver>();

                services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
                services.AddControllers();

                services.PostConfigureAll<MvcOptions>(options =>
                {
                    options.ModelMetadataDetailsProviders.Add(new StringEntityBindingMetadataProvider<User>());
                    options.ModelMetadataDetailsProviders.Add(new StringCollectionBindingMetadataProvider<User>());
                });
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
            });

        var server = new TestServer(builder);
        _client = server.CreateClient();
        _fixture = fixture;
    }

    [RetryFact(maxRetries: 10, delayBetweenRetriesMs: 1000)]
    public async Task CanBindFromRouteToUserEntityViaString()
    {
        // Get a seeded user from Cosmos DB
        var iterator = _fixture.UsersContainer.GetItemLinqQueryable<User>(true).ToFeedIterator();
        var response = await iterator.ReadNextAsync();
        var expectedUser = response.Resource.FirstOrDefault();
        
        Assert.NotNull(expectedUser);

        var userId = expectedUser!.Id.ToString();

        var httpResponse = await _client.GetAsync($"/api/users/{userId}");
        httpResponse.EnsureSuccessStatusCode();

        var json = await httpResponse.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<User>(json, _jsonOptions);

        Assert.Equal(expectedUser.Id, result!.Id);
        Assert.Equal(expectedUser.Name, result.Name);
    }

    [RetryFact(maxRetries: 10, delayBetweenRetriesMs: 1000)]
    public async Task CanFetchMultipleUsersByHttpRequest()
    {
        var iterator = _fixture.UsersContainer.GetItemLinqQueryable<User>(true).ToFeedIterator();
        var users = new List<User>();
        
        while (iterator.HasMoreResults && users.Count < 2)
        {
            var response = await iterator.ReadNextAsync();
            users.AddRange(response.Resource);
        }

        Assert.True(users.Count >= 2, "Need at least 2 users for this test");

        var idsCsv = string.Join(",", users.Select(u => u.Id));

        var httpResponse = await _client.GetAsync($"/api/users/batch/{idsCsv}");
        httpResponse.EnsureSuccessStatusCode();

        var json = await httpResponse.Content.ReadAsStringAsync();
        var returnedUsers = JsonSerializer.Deserialize<List<User>>(json, _jsonOptions);

        Assert.NotNull(returnedUsers);
        Assert.Equal(users.Count, returnedUsers!.Count);

        foreach (var expectedUser in users)
        {
            var actualUser = returnedUsers.FirstOrDefault(u => u.Id == expectedUser.Id);
            Assert.NotNull(actualUser);
            Assert.Equal(expectedUser.Name, actualUser.Name);
            Assert.Equal(expectedUser.Age, actualUser.Age);
        }
    }
}
