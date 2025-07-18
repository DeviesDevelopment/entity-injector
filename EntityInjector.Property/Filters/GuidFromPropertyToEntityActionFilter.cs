using EntityInjector.Property.Exceptions;
using Microsoft.Extensions.Logging;

namespace EntityInjector.Property.Filters;

public class GuidFromPropertyToEntityActionFilter(
    IServiceProvider serviceProvider,
    ILogger<GuidFromPropertyToEntityActionFilter> logger)
    : FromPropertyToEntityActionFilter<Guid>(serviceProvider, logger)
{
    protected override Guid ConvertToKey(object rawValue) => rawValue switch
    {
        Guid g => g,
        string s when Guid.TryParse(s, out var parsed) => parsed,
        _ => throw new InternalServerErrorException("bad mapping")
    };

    protected override Guid GetDefaultValueForNull() => Guid.Empty;
}
