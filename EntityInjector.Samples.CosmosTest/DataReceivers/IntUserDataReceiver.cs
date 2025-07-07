using EntityInjector.Route.Interfaces;
using EntityInjector.Samples.CosmosTest.Setup;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using User = EntityInjector.Samples.CosmosTest.Models.User;

namespace EntityInjector.Samples.CosmosTest.DataReceivers;

public class IntUserDataReceiver(CosmosContainer<User> cosmosContainer) : IBindingModelDataReceiver<int, User>
{
    private readonly Container _container = cosmosContainer.Container;
    public async Task<User?> GetByKey(int key, HttpContext httpContext, Dictionary<string, string> metaData)
    {
        var query = _container.GetItemLinqQueryable<User>(true)
            .Where(u => u.Age == key)
            .ToFeedIterator();

        if (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            return response.Resource.FirstOrDefault();
        }

        return null;
    }

    public async Task<Dictionary<int, User>> GetByKeys(List<int> keys, HttpContext httpContext, Dictionary<string, string> metaData)
    {
        var query = _container.GetItemLinqQueryable<User>(true)
            .Where(u => keys.Contains(u.Age))
            .ToFeedIterator();

        var result = new Dictionary<int, User>();
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            foreach (var user in response.Resource)
            {
                result[user.Age] = user;
            }
        }

        return result;
    }
}