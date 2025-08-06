using System.Net.Http.Json;
using System.Text.Json;
using EntityInjector.Core.Exceptions;
using EntityInjector.Core.Exceptions.Middleware;
using EntityInjector.Core.Interfaces;
using EntityInjector.Property.Filters;
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
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace EntityInjector.Samples.PostgresTest.Tests.Property;

public class PropertyExceptionTests : IClassFixture<PostgresTestFixture>
{
    private readonly PostgresTestFixture _fixture;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PropertyExceptionTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    private HttpClient CreateClient(Action<IServiceCollection>? overrideServices = null)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(_fixture.DbContext);
                services.AddEntityBinding();

                services.AddScoped<IBindingModelDataReceiver<Guid, User>, GuidUserDataReceiver>();

                services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
                services.AddControllers();

                services.PostConfigureAll<MvcOptions>(options =>
                {
                    options.Filters.Add<GuidFromPropertyToEntityActionFilter>();
                });

                services.PostConfigureAll<SwaggerGenOptions>(o =>
                {
                    o.SchemaFilter<FromPropertyToEntitySchemaFilter>();
                });

                // Apply test-specific overrides
                overrideServices?.Invoke(services);
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEntityBinding();
                app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
            });

        return new TestServer(builder).CreateClient();
    }


    [Fact]
    public async Task Returns404_WhenUserDoesNotExist()
    {
        var client = CreateClient();
        var payload = new { name = "Whiskers", ownerId = Guid.NewGuid() };

        var response = await client.PostAsJsonAsync("/api/pets", payload);

        var body = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, _jsonOptions);

        var expected = new EntityNotFoundException(nameof(PetModel.Owner), payload.ownerId);

        Assert.NotNull(problem);
        Assert.Equal(expected.StatusCode, problem!.Status);
        Assert.Equal(expected.Message, problem.Detail);
    }
    
    [Fact]
    public async Task Returns500_WhenNoBindingReceiverRegistered()
    {
        var client = CreateClient(services =>
        {
            // Remove existing receiver registration
            var descriptor = services.Single(d =>
                d.ServiceType == typeof(IBindingModelDataReceiver<Guid, User>));
            services.Remove(descriptor);
        });

        var payload = new { name = "Whiskers", ownerId = Guid.NewGuid() };
        var response = await client.PostAsJsonAsync("/api/pets", payload);
        var body = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, _jsonOptions);

        var expected = new BindingReceiverNotRegisteredException(typeof(IBindingModelDataReceiver<Guid, User>));
        
        Assert.NotNull(problem);
        Assert.Equal(expected.StatusCode, problem.Status);
        Assert.Equal(expected.Message, problem.Detail);
    }

    
}