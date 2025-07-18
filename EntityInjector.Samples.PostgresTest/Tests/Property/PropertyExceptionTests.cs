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
    
    [Fact]
    public async Task CanHydrateUserEntityFromOwnerIdOnPost()
    {
        // Arrange
        var expectedUser = await _fixture.DbContext.Users.FirstAsync();

        var petModel = new PetModel
        {
            Id = Guid.NewGuid(),
            Name = "Devon",
            Species = "Cat",
            OwnerId = expectedUser.Id
        };

        var json = JsonSerializer.Serialize(petModel);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/pets", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var returned = JsonSerializer.Deserialize<PetDto>(responseBody, _jsonOptions);

        // Assert
        Assert.NotNull(returned);
        Assert.Equal(petModel.Id, returned!.Id);
        Assert.Equal(petModel.Name, returned.Name);
        Assert.Equal(petModel.Species, returned.Species);

        Assert.NotNull(returned.Owner);
        Assert.Equal(expectedUser.Id, returned.Owner!.Id);
        Assert.Equal(expectedUser.Name, returned.Owner.Name);
    }

    [Fact]
    public async Task CanHydrateUserEntitiesFromOwnerIdsOnPostList()
    {
        // Arrange
        var expectedUser = await _fixture.DbContext.Users.FirstAsync();

        var petModels = new List<PetModel>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Bella",
                Species = "Dog",
                OwnerId = expectedUser.Id
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Max",
                Species = "Dog",
                OwnerId = expectedUser.Id
            }
        };

        var json = JsonSerializer.Serialize(petModels);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/pets/bulk", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var returned = JsonSerializer.Deserialize<List<PetDto>>(responseBody, _jsonOptions);

        // Assert
        Assert.NotNull(returned);
        Assert.Equal(2, returned!.Count);

        foreach (var pet in returned)
        {
            Assert.Equal(expectedUser.Id, pet.Owner!.Id);
            Assert.Equal(expectedUser.Name, pet.Owner.Name);
        }
    }
    
    [Fact]
    public async Task CanHydrateUserEntitiesFromDictionaryOnPost()
    {
        // Arrange
        var expectedUser = await _fixture.DbContext.Users.FirstAsync();

        var petDict = new Dictionary<string, PetModel>
        {
            ["bella"] = new PetModel
            {
                Id = Guid.NewGuid(),
                Name = "Bella",
                Species = "Dog",
                OwnerId = expectedUser.Id
            },
            ["max"] = new PetModel
            {
                Id = Guid.NewGuid(),
                Name = "Max",
                Species = "Dog",
                OwnerId = expectedUser.Id
            }
        };

        var json = JsonSerializer.Serialize(petDict);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/pets/by-name", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var returned = JsonSerializer.Deserialize<Dictionary<string, PetDto>>(responseBody, _jsonOptions);

        // Assert
        Assert.NotNull(returned);
        Assert.Equal(2, returned!.Count);

        foreach (var pet in returned.Select(entry => entry.Value))
        {
            Assert.Equal(expectedUser.Id, pet.Owner!.Id);
            Assert.Equal(expectedUser.Name, pet.Owner.Name);
        }
    }
    
    [Fact]
    public async Task CanHydrateUserEntitiesFromListOnPost()
    {
        // Arrange
        var expectedUsers = await _fixture.DbContext.Users.Take(2).ToListAsync();

        var project = new ProjectModel
        {
            Id = Guid.NewGuid(),
            Name = "Alpha",
            LeadIds = expectedUsers.Select(u => u.Id).ToList()
        };

        var json = JsonSerializer.Serialize(project);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/projects", content);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var returned = JsonSerializer.Deserialize<ProjectDto>(body, _jsonOptions);

        // Assert
        Assert.NotNull(returned);
        Assert.Equal(project.Id, returned!.Id);
        Assert.Equal(project.Name, returned.Name);
        Assert.Equal(2, returned.Leads.Count);

        foreach (var expected in expectedUsers)
        {
            Assert.Contains(returned.Leads, l => l.Id == expected.Id && l.Name == expected.Name);
        }
    }
    
    [Fact]
    public async Task CanHandleNullableForeignKey_AsNull_SingleEntity()
    {
        var petModel = new PetModelWithNullableOwner
        {
            Id = Guid.NewGuid(),
            Name = "Ghost",
            Species = "Dog",
            OwnerId = null
        };

        var json = JsonSerializer.Serialize(petModel);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/pets/nullable", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var returned = JsonSerializer.Deserialize<PetDto>(responseBody, _jsonOptions);

        Assert.NotNull(returned);
        Assert.Equal(petModel.Id, returned!.Id);
        Assert.Null(returned.Owner);
    }

    [Fact]
    public async Task CanHydrateNullableUserListFromNullableGuids()
    {
        // Arrange
        var users = await _fixture.DbContext.Users.Take(3).ToListAsync();
        var leadIds = new List<Guid?> { users[0].Id, Guid.NewGuid(), users[2].Id };

        var model = new ProjectModelWithNullableLeads
        {
            Id = Guid.NewGuid(),
            Name = "NullSafe Project",
            LeadIds = leadIds
        };

        var json = JsonSerializer.Serialize(model);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/projects/nullable", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var returned = JsonSerializer.Deserialize<ProjectDto>(responseBody, _jsonOptions);

        // Assert
        Assert.NotNull(returned);
        Assert.Equal(model.Id, returned!.Id);
        Assert.Equal(model.Name, returned.Name);

        Assert.NotNull(returned.Leads);
        Assert.Equal(3, returned.Leads.Count);
        Assert.Equal(users[0].Id, returned.Leads[0]!.Id);
        Assert.Null(returned.Leads[1]);
        Assert.Equal(users[2].Id, returned.Leads[2]!.Id);
    }


}
