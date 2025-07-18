using System.Text.Json;
using EntityInjector.Core.Interfaces;
using EntityInjector.Route.BindingMetadata.Collection;
using EntityInjector.Route.BindingMetadata.Entity;
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

public class CosmosMultipleModelsTests : IClassFixture<CosmosTestFixture>
{
    private readonly HttpClient _client;
    private readonly CosmosTestFixture _fixture;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    public CosmosMultipleModelsTests(CosmosTestFixture fixture)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(fixture.Client);
                services.AddSingleton(new CosmosContainer<User>(fixture.UsersContainer));
                services.AddSingleton(new CosmosContainer<Product>(fixture.ProductsContainer));

                services.AddScoped<IBindingModelDataReceiver<Guid, User>, GuidUserDataReceiver>();
                services.AddScoped<IBindingModelDataReceiver<int, Product>, IntProductDataReceiver>();

                services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
                services.AddControllers();

                services.PostConfigureAll<MvcOptions>(options =>
                {
                    options.ModelMetadataDetailsProviders.Add(new GuidEntityBindingMetadataProvider<User>());
                    options.ModelMetadataDetailsProviders.Add(new GuidCollectionBindingMetadataProvider<User>());

                    options.ModelMetadataDetailsProviders.Add(new IntEntityBindingMetadataProvider<Product>());
                    options.ModelMetadataDetailsProviders.Add(new IntCollectionBindingMetadataProvider<Product>());
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

    private async Task<List<User>> GetSeededUsersAsync()
    {
        var iterator = _fixture.UsersContainer.GetItemLinqQueryable<User>().ToFeedIterator();
        var users = new List<User>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            users.AddRange(response);
        }

        return users;
    }

    private async Task<List<Product>> GetSeededProductsAsync()
    {
        var iterator = _fixture.ProductsContainer.GetItemLinqQueryable<Product>().ToFeedIterator();
        var products = new List<Product>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            products.AddRange(response);
        }

        return products;
    }

    [RetryFact(maxRetries: 10, delayBetweenRetriesMs: 1000)]
    public async Task CanBindFromRouteToUserEntityViaGuid()
    {
        var users = await GetSeededUsersAsync();
        var expectedUser = users.First();

        var response = await _client.GetAsync($"/api/users/{expectedUser.Id}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<User>(json, _jsonOptions);

        Assert.Equal(expectedUser.Id, result!.Id);
        Assert.Equal(expectedUser.Name, result.Name);
        Assert.Equal(expectedUser.Age, result.Age);
    }

    [RetryFact(maxRetries: 10, delayBetweenRetriesMs: 1000)]
    public async Task CanBindFromRouteToProductEntityViaInt()
    {
        var products = await GetSeededProductsAsync();
        var expectedProduct = products.First();

        var response = await _client.GetAsync($"/api/products/{expectedProduct.Id}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Product>(json, _jsonOptions);

        Assert.Equal(expectedProduct.Id, result!.Id);
        Assert.Equal(expectedProduct.Name, result.Name);
        Assert.Equal(expectedProduct.Price, result.Price);
    }

    [RetryFact(maxRetries: 10, delayBetweenRetriesMs: 1000)]
    public async Task CanFetchMultipleUsersByHttpRequest()
    {
        var users = (await GetSeededUsersAsync()).Take(2).ToList();
        var idsCsv = string.Join(",", users.Select(u => u.Id));

        var response = await _client.GetAsync($"/api/users/batch/{idsCsv}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
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

    [RetryFact(maxRetries: 10, delayBetweenRetriesMs: 1000)]
    public async Task CanFetchMultipleProductsByHttpRequest()
    {
        var products = (await GetSeededProductsAsync()).Take(2).ToList();
        var idsCsv = string.Join(",", products.Select(p => p.Id));

        var response = await _client.GetAsync($"/api/products/batch/{idsCsv}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var returnedProducts = JsonSerializer.Deserialize<List<Product>>(json, _jsonOptions);

        Assert.NotNull(returnedProducts);
        Assert.Equal(products.Count, returnedProducts!.Count);

        foreach (var expectedProduct in products)
        {
            var actualProduct = returnedProducts.FirstOrDefault(p => p.Id == expectedProduct.Id);
            Assert.NotNull(actualProduct);
            Assert.Equal(expectedProduct.Name, actualProduct.Name);
            Assert.Equal(expectedProduct.Price, actualProduct.Price);
        }
    }
}
