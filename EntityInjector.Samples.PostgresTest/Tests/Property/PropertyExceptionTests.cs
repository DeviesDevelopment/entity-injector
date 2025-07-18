using System.Text;
using System.Text.Json;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace EntityInjector.Samples.PostgresTest.Tests.Property;

public class PropertyExceptionTests : IClassFixture<PostgresTestFixture>
{
    private readonly HttpClient _client;
    private readonly PostgresTestFixture _fixture;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PropertyExceptionTests(PostgresTestFixture fixture)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(fixture.DbContext);

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
}