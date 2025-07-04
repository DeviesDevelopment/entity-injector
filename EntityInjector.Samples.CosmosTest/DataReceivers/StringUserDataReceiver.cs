using EntityInjector.Route.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using User = EntityInjector.Samples.CosmosTest.Models.User;

namespace EntityInjector.Samples.CosmosTest.DataReceivers;

public class StringUserDataReceiver(Container container) : IBindingModelDataReceiver<string, User>
{
    public async Task<User?> GetByKey(string key, HttpContext httpContext, Dictionary<string, string> metaData)
    {
        try
        {
            var response = await container.ReadItemAsync<User>(key, PartitionKey.None);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Dictionary<string, User>> GetByKeys(List<string> keys, HttpContext httpContext, Dictionary<string, string> metaData)
    {
        var result = new Dictionary<string, User>();

        foreach (var key in keys)
        {
            var user = await GetByKey(key, httpContext, metaData);
            if (user != null)
            {
                result[key] = user;
            }
        }

        return result;
    }
}