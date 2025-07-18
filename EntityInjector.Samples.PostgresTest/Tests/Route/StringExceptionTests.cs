using System.Net;
using System.Text.Json;
using EntityInjector.Core.Exceptions.Middleware;
using EntityInjector.Core.Interfaces;
using EntityInjector.Route.BindingMetadata.Collection;
using EntityInjector.Route.BindingMetadata.Entity;
using EntityInjector.Route.Exceptions;
using EntityInjector.Samples.PostgresTest.DataReceivers;
using EntityInjector.Samples.PostgresTest.Models;
using EntityInjector.Samples.PostgresTest.Models.Entities;
using EntityInjector.Samples.PostgresTest.Setup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EntityInjector.Samples.PostgresTest.Tests.Route;

public class StringExceptionTests : IClassFixture<PostgresTestFixture>
{
    private readonly HttpClient _client;
    private readonly PostgresTestFixture _fixture;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    public StringExceptionTests(PostgresTestFixture fixture)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(fixture.DbContext);
                services.AddRouteBinding();
                // Use only one type of FromRoute bindings per Value type to avoid ambiguous bindings
                services.AddScoped<IBindingModelDataReceiver<string, User>, StringUserDataReceiver>();

                services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
                services.AddControllers();

                services.PostConfigureAll<MvcOptions>(options =>
                {
                    // Use only one type of FromRoute bindings per Value type to avoid ambiguous bindings
                    options.ModelMetadataDetailsProviders.Add(new StringEntityBindingMetadataProvider<User>());
                    options.ModelMetadataDetailsProviders.Add(new StringCollectionBindingMetadataProvider<User>());
                    
                    // Add Product binding metadata, but omit the receiver on purpose
                    options.ModelMetadataDetailsProviders.Add(new IntEntityBindingMetadataProvider<Product>());
                });
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseRouteBinding();
                app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
            });

        var server = new TestServer(builder);
        _client = server.CreateClient();
        _fixture = fixture;
    }

    [Fact]
    public async Task ReturnsNotFoundForNonexistentUserId()
    {
        // Arrange: Use a random GUID to ensure it doesn't exist
        var nonexistentUserId = Guid.NewGuid().ToString();
        var requestUri = $"/api/users/{nonexistentUserId}";

        // Act
        var response = await _client.GetAsync(requestUri);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, _jsonOptions);

        var expected = new RouteEntityNotFoundException("User", nonexistentUserId);
        
        Assert.NotNull(problem);
        Assert.Equal(expected.StatusCode, problem!.Status);
        Assert.Contains(expected.Message, problem.Detail);
        Assert.Equal(requestUri, problem!.Instance);
    }
    
    [Fact]
    public async Task ReturnsBadRequestWhenRouteParameterIsMissing()
    {
        // Arrange
        var requestUri = "/api/invalid/users/"; // missing route value

        // Act
        var response = await _client.GetAsync(requestUri);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, _jsonOptions);

        var expected = new MissingRouteParameterException("id");
        
        Assert.NotNull(problem);
        Assert.Equal(expected.StatusCode, problem!.Status);
        Assert.Equal(expected.Message, problem.Detail);
        Assert.Equal(requestUri, problem.Instance);
    }

    
    [Fact]
    public async Task ReturnsInternalServerErrorWhenNoReceiverIsRegistered()
    {
        // Arrange
        var requestUri = $"/api/products/{9999}";

        // Act
        var response = await _client.GetAsync(requestUri);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, _jsonOptions);

        var expected = new BindingReceiverNotRegisteredException(typeof(IBindingModelDataReceiver<int, Product>));

        Assert.NotNull(problem);
        Assert.Equal(expected.StatusCode, problem!.Status);
        Assert.Equal(expected.Message, problem.Detail);
        Assert.Equal(requestUri, problem.Instance);
    }
    
    [Fact]
    public async Task ReturnsBadRequestWhenRouteCollectionParameterIsEmpty()
    {
        var requestUri = "/api/invalid/users/batch/,,,";

        var response = await _client.GetAsync(requestUri);

        var body = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, _jsonOptions);

        var expected = new EmptyRouteSegmentListException("ids");

        Assert.NotNull(problem);
        Assert.Equal(expected.StatusCode, problem!.Status);
        Assert.Equal(expected.Message, problem.Detail);
    }
}