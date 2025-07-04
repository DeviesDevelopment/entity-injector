using System.Text.Json;
using EntityInjector.Route.Interfaces;
using EntityInjector.Route.Middleware.BindingMetadata.Collection;
using EntityInjector.Route.Middleware.BindingMetadata.Entity;
using EntityInjector.Samples.PostgresTest.DataReceivers;
using EntityInjector.Samples.PostgresTest.Models;
using EntityInjector.Samples.PostgresTest.Setup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EntityInjector.Samples.PostgresTest.Tests;

public class MultipleModelsTests : IClassFixture<PostgresTestFixture>
{
    private readonly HttpClient _client;
    private readonly PostgresTestFixture _fixture;

    public MultipleModelsTests(PostgresTestFixture fixture)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(fixture.DbContext);

                // Use only one type of FromRoute bindings per Value type to avoid ambiguous bindings
                services.AddScoped<IBindingModelDataReceiver<Guid, User>, GuidUserDataReceiver>();
                services.AddScoped<IBindingModelDataReceiver<int, Product>, IntProductDataReceiver>();

                services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
                services.AddControllers();

                services.PostConfigureAll<MvcOptions>(options =>
                {
                    // Use only one type of FromRoute bindings per Value type to avoid ambiguous bindings
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

    [Fact]
    public async Task CanBindFromRouteToUserEntityViaGuid()
    {
        var expectedUser = await _fixture.DbContext.Users.FirstAsync();
        var userId = expectedUser.Id;

        Assert.NotNull(expectedUser);

        var response = await _client.GetAsync($"/api/users/{userId}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<User>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.Equal(expectedUser.Id, result!.Id);
        Assert.Equal(expectedUser.Name, result.Name);
    }


    [Fact]
    public async Task CanBindFromRouteToProductEntityViaInt()
    {
        var expectedProduct = await _fixture.DbContext.Products.FirstAsync();
        var productId = expectedProduct.Id;

        Assert.NotNull(expectedProduct);

        var response = await _client.GetAsync($"/api/products/{productId}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Product>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.Equal(expectedProduct.Id, result!.Id);
        Assert.Equal(expectedProduct.Name, result.Name);
    }

    [Fact]
    public async Task CanFetchMultipleUsersByHttpRequest()
    {
        var dbContext = _fixture.DbContext;
        var users = await dbContext.Users.Take(2).ToListAsync();

        Assert.True(users.Count >= 2, "Need at least 2 users for this test");

        var idsCsv = string.Join(",", users.Select(u => u.Id));

        var response = await _client.GetAsync($"/api/users/batch/{idsCsv}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var returnedUsers = JsonSerializer.Deserialize<List<User>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

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

    [Fact]
    public async Task CanFetchMultipleProductsByHttpRequest()
    {
        var dbContext = _fixture.DbContext;
        var products = await dbContext.Products.Take(2).ToListAsync();

        Assert.True(products.Count >= 2, "Need at least 2 users for this test");

        var idsCsv = string.Join(",", products.Select(u => u.Id));

        var response = await _client.GetAsync($"/api/products/batch/{idsCsv}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var returnedProducts = JsonSerializer.Deserialize<List<Product>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(returnedProducts);
        Assert.Equal(products.Count, returnedProducts!.Count);

        foreach (var expectedProduct in products)
        {
            var actualUser = returnedProducts.FirstOrDefault(p => p.Id == expectedProduct.Id);
            Assert.NotNull(actualUser);
            Assert.Equal(expectedProduct.Name, actualUser.Name);
            Assert.Equal(expectedProduct.Price, actualUser.Price);
        }
    }
}