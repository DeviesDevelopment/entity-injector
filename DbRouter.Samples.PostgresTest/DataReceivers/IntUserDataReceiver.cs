using DbRouter.Interfaces;
using DbRouter.Samples.PostgresTest.Models;
using DbRouter.Samples.PostgresTest.Setup;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DbRouter.Samples.PostgresTest.DataReceivers;

public class IntUserDataReceiver(TestDbContext db) : IBindingModelDataReceiver<int, User>
{
    public Task<User?> GetByKey(int key, HttpContext httpContext, Dictionary<string, string> metaData)
    {
        return db.Users.Where(u => u.Age == key).FirstOrDefaultAsync();
    }
    
    public Task<Dictionary<int, User>> GetByKeys(List<int> keys, HttpContext httpContext, Dictionary<string, string> metaData)
    {
        return db.Users.Where(u => keys.Contains(u.Age)).ToDictionaryAsync(u => u.Age);
    }
}
