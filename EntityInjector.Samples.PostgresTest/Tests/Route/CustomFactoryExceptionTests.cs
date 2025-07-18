using System.Text.Json;
using EntityInjector.Core.Exceptions;
using EntityInjector.Core.Exceptions.Middleware;
using EntityInjector.Core.Interfaces;
using EntityInjector.Route.BindingMetadata.Entity;
using EntityInjector.Route.Filters;
using EntityInjector.Samples.PostgresTest.DataReceivers;
using EntityInjector.Samples.PostgresTest.Models.Entities;
using EntityInjector.Samples.PostgresTest.Setup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace EntityInjector.Samples.PostgresTest.Tests.Route;

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
                services.TryAddSingleton<IEntityBindingProblemDetailsFactory, CustomEntityBindingProblemDetailsFactory>();
                
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
                services.PostConfigureAll<SwaggerGenOptions>(o =>
                {
                    o.OperationFilter<FromRouteToEntityOperationFilter>();
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
    // unless the exception is EntityNotFoundException on a Guid with the Entity User 
    public class CustomEntityBindingProblemDetailsFactory : IEntityBindingProblemDetailsFactory
    {
        public ProblemDetails Create(HttpContext context, EntityBindingException exception)
        {
            var problem = new ProblemDetails
            {
                Status = exception.StatusCode,
                Instance = context.Request.Path
            };

            if (exception is EntityNotFoundException { EntityName: "User" })
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

        var expected = new InvalidEntityParameterFormatException("id", typeof(Guid), typeof(string));
        
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

        var expected = new EntityNotFoundException("User", userId);
        
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

        var expected = new EntityNotFoundException("Product", productId);
        
        Assert.NotNull(problem);
        Assert.Equal(expected.StatusCode, problem!.Status);
        Assert.Null(problem.Detail);
    }
}