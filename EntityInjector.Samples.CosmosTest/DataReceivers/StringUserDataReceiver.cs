using System.Net;
using EntityInjector.Core.Interfaces;
using EntityInjector.Samples.CosmosTest.Setup;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using User = EntityInjector.Samples.CosmosTest.Models.User;

namespace EntityInjector.Samples.CosmosTest.DataReceivers;

public class StringUserDataReceiver(CosmosContainer<User> cosmosContainer) : IBindingModelDataReceiver<string, User>
{
    private readonly Container _container = cosmosContainer.Container;

    public async Task<User?> GetByKey(string key, HttpContext httpContext, Dictionary<string, string> metaData)
    {
        try
        {
            var pk = new PartitionKey(key);
            var response = await _container.ReadItemAsync<User>(key, pk);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Dictionary<string, User>> GetByKeys(List<string> keys, HttpContext httpContext,
        Dictionary<string, string> metaData)
    {
        var result = new Dictionary<string, User>();

        foreach (var key in keys)
        {
            var user = await GetByKey(key, httpContext, metaData);
            if (user != null) result[key] = user;
        }

        return result;
    }
}