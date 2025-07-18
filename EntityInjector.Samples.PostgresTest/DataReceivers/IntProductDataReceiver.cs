using EntityInjector.Core.Interfaces;
using EntityInjector.Samples.PostgresTest.Models.Entities;
using EntityInjector.Samples.PostgresTest.Setup;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EntityInjector.Samples.PostgresTest.DataReceivers;

public class IntProductDataReceiver(TestDbContext db) : IBindingModelDataReceiver<int, Product>
{
    public Task<Product?> GetByKey(int key, HttpContext httpContext, Dictionary<string, string> metaData)
    {
        return db.Products.FindAsync(key).AsTask();
    }

    public Task<Dictionary<int, Product>> GetByKeys(List<int> keys, HttpContext httpContext,
        Dictionary<string, string> metaData)
    {
        return db.Products.Where(p => keys.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
    }
}