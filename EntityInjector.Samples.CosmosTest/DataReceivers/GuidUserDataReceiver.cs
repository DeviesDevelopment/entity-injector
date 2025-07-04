using EntityInjector.Route.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using User = EntityInjector.Samples.CosmosTest.Models.User;

namespace EntityInjector.Samples.CosmosTest.DataReceivers;

public class GuidUserDataReceiver(Container container) : IBindingModelDataReceiver<Guid, User>
{
    public async Task<User?> GetByKey(Guid key, HttpContext httpContext, Dictionary<string, string> metaData)
    {
        try
        {
            var response = await container.ReadItemAsync<User>(key.ToString(), PartitionKey.None);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Dictionary<Guid, User>> GetByKeys(List<Guid> keys, HttpContext httpContext, Dictionary<string, string> metaData)
    {
        var result = new Dictionary<Guid, User>();

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