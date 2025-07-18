using EntityInjector.Core.Interfaces;
using EntityInjector.Samples.CosmosTest.Models;
using EntityInjector.Samples.CosmosTest.Setup;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace EntityInjector.Samples.CosmosTest.DataReceivers;

public class IntProductDataReceiver(CosmosContainer<Product> cosmosContainer) : IBindingModelDataReceiver<int, Product>
{
    private readonly Container _container = cosmosContainer.Container;
    
    public async Task<Product?> GetByKey(int key, HttpContext httpContext, Dictionary<string, string> metaData)
    {
        var query = _container.GetItemLinqQueryable<Product>(true)
            .Where(p => p.Id == key.ToString())
            .ToFeedIterator();

        if (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            return response.Resource.FirstOrDefault();
        }

        return null;
    }

    public async Task<Dictionary<int, Product>> GetByKeys(List<int> keys, HttpContext httpContext, Dictionary<string, string> metaData)
    {
        var stringKeys = keys.Select(k => k.ToString()).ToList();

        var query = _container.GetItemLinqQueryable<Product>(true)
            .Where(p => stringKeys.Contains(p.Id))
            .ToFeedIterator();

        var result = new Dictionary<int, Product>();
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            foreach (var product in response.Resource)
            {
                result[int.Parse(product.Id)] = product;
            }
        }

        return result;
    }
}