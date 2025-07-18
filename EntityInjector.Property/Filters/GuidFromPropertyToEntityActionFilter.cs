using EntityInjector.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace EntityInjector.Property.Filters;

public class GuidFromPropertyToEntityActionFilter(
    IServiceProvider serviceProvider,
    ILogger<GuidFromPropertyToEntityActionFilter> logger)
    : FromPropertyToEntityActionFilter<Guid>(serviceProvider, logger)
{
    protected override Guid ConvertToKey(object rawValue)
    {
        return rawValue switch
        {
            Guid g => g,
            string s when Guid.TryParse(s, out var parsed) => parsed,
            _ => throw new InvalidEntityParameterFormatException("id", typeof(Guid), rawValue.GetType())
        };
    }

    protected override Guid GetDefaultValueForNull()
    {
        return Guid.Empty;
    }
}