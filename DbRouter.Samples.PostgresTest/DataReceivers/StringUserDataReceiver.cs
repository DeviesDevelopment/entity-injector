using DbRouter.Interfaces;
using DbRouter.Samples.PostgresTest.Models;
using DbRouter.Samples.PostgresTest.Setup;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DbRouter.Samples.PostgresTest.DataReceivers;

public class StringUserDataReceiver(TestDbContext db) : IBindingModelDataReceiver<string, User>
{
    public Task<User?> GetByKey(string key, HttpContext httpContext, Dictionary<string, string> metaData)
    {
        return db.Users.FindAsync(Guid.Parse(key)).AsTask();
    }
    
    public Task<Dictionary<string, User>> GetByKeys(List<string> keys, HttpContext httpContext, Dictionary<string, string> metaData)
    {
        var parsedKeys = keys.Select(Guid.Parse);
        return db.Users.Where(u => parsedKeys.Contains(u.Id)).ToDictionaryAsync(u => u.Id.ToString());
    }
}
