using Microsoft.AspNetCore.Http;

namespace EntityInjector.Route.Interfaces;

public interface IBindingModelDataReceiver<TKey, TType> where TKey : notnull
{
    Task<TType?> GetByKey(TKey key, HttpContext httpContext, Dictionary<string, string> metaData);

    Task<Dictionary<TKey, TType>> GetByKeys(List<TKey> keys, HttpContext httpContext,
        Dictionary<string, string> metaData);
}