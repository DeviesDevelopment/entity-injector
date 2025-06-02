using DbRouter.Interfaces;
using DbRouter.Samples.PostgresTest.Models;
using DbRouter.Samples.PostgresTest.Setup;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DbRouter.Samples.PostgresTest.DataReceivers;

public class GuidUserDataReceiver(TestDbContext db) : IBindingModelDataReceiver<Guid, User>
{
    public Task<User?> GetByKey(Guid key, HttpContext httpContext, Dictionary<string, string> metaData)
    {
        return db.Users.FindAsync(key).AsTask();
    }
    
    public Task<Dictionary<Guid, User>> GetByKeys(List<Guid> keys, HttpContext httpContext, Dictionary<string, string> metaData)
    {
        return db.Users.Where(u => keys.Contains(u.Id)).ToDictionaryAsync(u => u.Id);
    }
}
