using EntityInjector.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace EntityInjector.Property.Filters;

public class StringFromPropertyToEntityActionFilter(
    IServiceProvider serviceProvider,
    ILogger<StringFromPropertyToEntityActionFilter> logger)
    : FromPropertyToEntityActionFilter<string>(serviceProvider, logger)
{
    protected override string ConvertToKey(object rawValue, string propertyName)
    {
        return rawValue switch
        {
            string a => a,
            Guid g => g.ToString(),
            _ => throw new InvalidEntityParameterFormatException(propertyName, typeof(string), rawValue.GetType())
        };
    }

    protected override string GetDefaultValueForNull()
    {
        return "";
    }
}