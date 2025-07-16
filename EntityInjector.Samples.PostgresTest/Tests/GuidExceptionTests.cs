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
using Xunit;

namespace EntityInjector.Samples.PostgresTest.Tests;

public class GuidExceptionTests : IClassFixture<PostgresTestFixture>
{
    private readonly HttpClient _client;
    private readonly PostgresTestFixture _fixture;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    public GuidExceptionTests(PostgresTestFixture fixture)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(fixture.DbContext);
                services.AddRouteBinding();
                // Use only one type of FromRoute bindings per Value type to avoid ambiguous bindings
                services.AddScoped<IBindingModelDataReceiver<Guid, User>, GuidUserDataReceiver>();

                services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
                services.AddControllers();

                services.PostConfigureAll<MvcOptions>(options =>
                {
                    // Use only one type of FromRoute bindings per Value type to avoid ambiguous bindings
                    options.ModelMetadataDetailsProviders.Add(new GuidEntityBindingMetadataProvider<User>());
                    options.ModelMetadataDetailsProviders.Add(new GuidCollectionBindingMetadataProvider<User>());
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
    public async Task ReturnsBadRequestWhenGuidRouteParameterIsMalformed()
    {
        var requestUri = "/api/invalid/users/not-a-guid";

        var response = await _client.GetAsync(requestUri);

        var body = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, _jsonOptions);

        var expected = new InvalidRouteParameterFormatException("id", typeof(Guid), typeof(string));
    
        Assert.NotNull(problem);
        Assert.Equal(expected.StatusCode, problem!.Status);
        Assert.Contains("id", problem.Detail);
    }
}