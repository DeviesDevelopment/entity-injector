using EntityInjector.Property.Exceptions;
using Microsoft.Extensions.Logging;

namespace EntityInjector.Property.Filters;

public class StringFromPropertyToEntityActionFilter(
    IServiceProvider serviceProvider,
    ILogger<StringFromPropertyToEntityActionFilter> logger)
    : FromPropertyToEntityActionFilter<string>(serviceProvider, logger)
{
    
    protected override string ConvertToKey(object rawValue) => rawValue switch
    {
        string a => a,
        Guid g => g.ToString(),
        _ => throw new InternalServerErrorException("bad mapping")
    };

    protected override string GetDefaultValueForNull() => "";
}