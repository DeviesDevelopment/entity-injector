using System.Net;
using System.Text.Json;
using EntityInjector.Route.Exceptions;
using EntityInjector.Route.Exceptions.Middleware;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace EntityInjector.Samples.PostgresTest.Tests;

public class CustomFactoryExceptionTests : IClassFixture<PostgresTestFixture>
{
    private readonly HttpClient _client;
    private readonly PostgresTestFixture _fixture;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    public CustomFactoryExceptionTests(PostgresTestFixture fixture)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(fixture.DbContext);
                services.TryAddSingleton<IRouteBindingProblemDetailsFactory, CustomRouteBindingProblemDetailsFactory>();
                
                // Use only one type of FromRoute bindings per Value type to avoid ambiguous bindings
                services.AddScoped<IBindingModelDataReceiver<Guid, User>, GuidUserDataReceiver>();
                services.AddScoped<IBindingModelDataReceiver<int, Product>, IntProductDataReceiver>();

                services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
                services.AddControllers();

                services.PostConfigureAll<MvcOptions>(options =>
                {
                    // Use only one type of FromRoute bindings per Value type to avoid ambiguous bindings
                    options.ModelMetadataDetailsProviders.Add(new GuidEntityBindingMetadataProvider<User>());
                    
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
    
    // Custom factory which does not include Detail,
    // unless the exception is RouteEntityNotFoundException on a Guid with the Entity User 
    public class CustomRouteBindingProblemDetailsFactory : IRouteBindingProblemDetailsFactory
    {
        public ProblemDetails Create(HttpContext context, RouteBindingException exception)
        {
            var problem = new ProblemDetails
            {
                Status = exception.StatusCode,
                Instance = context.Request.Path
            };

            if (exception is RouteEntityNotFoundException { EntityName: "User" })
            {
                problem.Detail = exception.Message;
            }

            return problem;
        }
    }
    
    [Fact]
    public async Task InvalidRouteParameterFormatException_HasNoDetail()
    {
        var requestUri = "/api/invalid/users/not-a-guid";

        var response = await _client.GetAsync(requestUri);

        var body = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, _jsonOptions);

        var expected = new InvalidRouteParameterFormatException("id", typeof(Guid), typeof(string));
        
        Assert.NotNull(problem);
        Assert.Equal(expected.StatusCode, problem!.Status);
        Assert.Null(problem.Detail);
    }

    [Fact]
    public async Task RouteEntityNotFoundException_ForUser_HasDetail()
    {
        var userId = Guid.NewGuid();
        var requestUri = $"/api/users/{userId}";

        var response = await _client.GetAsync(requestUri);

        var body = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, _jsonOptions);

        var expected = new RouteEntityNotFoundException("User", userId);
        
        Assert.NotNull(problem);
        Assert.Equal(expected.StatusCode, problem!.Status);
        Assert.NotNull(problem.Detail);
        Assert.Equal(expected.Message, problem.Detail);
    }
    
    [Fact]
    public async Task RouteEntityNotFoundException_ForProduct_HasNoDetail()
    {
        var productId = 9999;
        var requestUri = $"/api/products/{productId}";

        var response = await _client.GetAsync(requestUri);

        var body = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, _jsonOptions);

        var expected = new RouteEntityNotFoundException("Product", productId);
        
        Assert.NotNull(problem);
        Assert.Equal(expected.StatusCode, problem!.Status);
        Assert.Null(problem.Detail);
    }
}